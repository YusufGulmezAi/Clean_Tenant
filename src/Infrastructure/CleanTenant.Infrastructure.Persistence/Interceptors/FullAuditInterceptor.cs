using System.Reflection;
using System.Text.Json;
using CleanTenant.Application.Common.Auditing;
using CleanTenant.Domain.Auditing;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.SharedKernel.Entities;
using CleanTenant.SharedKernel.Time;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CleanTenant.Infrastructure.Persistence.Interceptors;

/// <summary>
/// <para>
/// Catalog SaveChanges öncesi ChangeTracker'dan tüm Added/Modified/Deleted
/// <see cref="IEntity"/>'leri toplar; delta JSON üretir (PII <c>"[REDACTED]"</c>);
/// SaveChanges başarılı olduktan sonra Dapper ile Audit DB'ye batch INSERT yapar.
/// </para>
/// <para>
/// Aktif <see cref="ApplicationAuditState.SupportSessionId"/> varsa, audit kaydı
/// işaretlenir + ilgili <see cref="SupportSession.WriteActionCount"/> Catalog
/// ChangeTracker'a track'lenip aynı transaction'da artırılır.
/// </para>
/// </summary>
public sealed class FullAuditInterceptor : SaveChangesInterceptor
{
    /// <summary>EF Core / Identity'den gelen PII kolonları — attribute olmadan da redact edilir.</summary>
    private static readonly HashSet<string> PiiPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "TokenHash",
        "RefreshTokenHash",
        "AuthenticatorKey",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IAuditMetadataAccessor _metadataAccessor;
    private readonly IClock _clock;
    private readonly string _auditConnectionString;

    /// <summary>Toplanmış audit entry'ler — SaveChanges sonrası yazılır.</summary>
    private readonly List<AuditEntry> _pendingEntries = [];

    /// <summary>DI bağımlılıklarını alır.</summary>
    public FullAuditInterceptor(
        IAuditMetadataAccessor metadataAccessor,
        IClock clock,
        string auditConnectionString)
    {
        _metadataAccessor = metadataAccessor;
        _clock = clock;
        _auditConnectionString = auditConnectionString;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            CollectEntries(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            CollectEntries(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingEntries.Count > 0)
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_pendingEntries.Count > 0)
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        // SaveChanges fail → audit yazımı atılır (Catalog state değişmedi, audit gereksiz).
        _pendingEntries.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendingEntries.Clear();
        base.SaveChangesFailed(eventData);
    }

    private void CollectEntries(DbContext context)
    {
        var metadata = _metadataAccessor.Capture();
        var now = _clock.UtcNow;

        // SupportSession.WriteActionCount artırımı (varsa)
        if (metadata.SupportSessionId is { } supportId)
        {
            IncrementSupportWriteCount(context, supportId);
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IEntity entity)
            {
                continue;
            }

            var action = ResolveAction(entry);
            if (action is null)
            {
                continue;
            }

            var changes = ExtractChanges(entry, action.Value);
            var auditEntry = BuildAuditEntry(entity, entry, action.Value, changes, metadata, now);
            _pendingEntries.Add(auditEntry);
        }
    }

    /// <summary>Soft-delete dönüşümü AuditingInterceptor tarafından yapılıyor — bizim için Modified+IsDeleted=true → Delete.</summary>
    private static AuditAction? ResolveAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditAction.Create;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditAction.Delete;
        }

        if (entry.State == EntityState.Modified)
        {
            // Soft-delete: IsDeleted false→true → Delete
            if (entry.Entity is ISoftDeletable && entry.Metadata.FindProperty(nameof(ISoftDeletable.IsDeleted)) is { } isDeletedProp)
            {
                var prop = entry.Property(isDeletedProp.Name);
                if (!Equals(prop.OriginalValue, prop.CurrentValue) && Equals(prop.CurrentValue, true))
                {
                    return AuditAction.Delete;
                }
            }
            return AuditAction.Update;
        }

        return null;
    }

    private static Dictionary<string, object?> ExtractChanges(EntityEntry entry, AuditAction action)
    {
        var changes = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            var isSensitive = IsSensitive(entry.Metadata.ClrType, name);

            switch (action)
            {
                case AuditAction.Create:
                    var newValue = isSensitive ? "[REDACTED]" : property.CurrentValue;
                    if (newValue is not null)
                    {
                        changes[name] = new { @new = newValue };
                    }
                    break;

                case AuditAction.Update:
                    if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                    {
                        changes[name] = new
                        {
                            old = isSensitive ? "[REDACTED]" : property.OriginalValue,
                            @new = isSensitive ? "[REDACTED]" : property.CurrentValue,
                        };
                    }
                    break;

                case AuditAction.Delete:
                    // Delete'te tek alan: kim sildi audit alanlarıyla zaten dolu; ana entity snapshot:
                    var oldValue = isSensitive ? "[REDACTED]" : property.OriginalValue;
                    if (oldValue is not null)
                    {
                        changes[name] = new { old = oldValue };
                    }
                    break;
            }
        }

        return changes;
    }

    private static bool IsSensitive(Type entityType, string propertyName)
    {
        if (PiiPropertyNames.Contains(propertyName))
        {
            return true;
        }

        var prop = entityType.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetCustomAttribute<SensitiveAttribute>(inherit: true) is not null;
    }

    private static AuditEntry BuildAuditEntry(
        IEntity entity,
        EntityEntry entry,
        AuditAction action,
        Dictionary<string, object?> changes,
        AuditMetadata metadata,
        DateTimeOffset now)
    {
        return new AuditEntry
        {
            Id = Guid.CreateVersion7(now),
            Timestamp = now,

            UserId = metadata.UserId,
            UserEmail = metadata.UserEmail,
            UserFullName = metadata.UserFullName,
            TenantId = metadata.TenantId,
            TenantName = metadata.TenantName,
            ScopeLevel = metadata.ScopeLevel,
            CompanyId = metadata.CompanyId,
            UnitId = metadata.UnitId,
            PersonaSide = metadata.PersonaSide,
            RolesJson = metadata.Roles.Count > 0 ? JsonSerializer.Serialize(metadata.Roles, JsonOptions) : null,

            IsSystemSession = metadata.IsSystemSession,
            SupportSessionId = metadata.SupportSessionId,
            ImpersonatedByUserId = metadata.ImpersonatedByUserId,

            IpAddress = metadata.IpAddress,
            UserAgent = metadata.UserAgent,
            BrowserName = metadata.BrowserName,
            BrowserVersion = metadata.BrowserVersion,
            OperatingSystem = metadata.OperatingSystem,
            DeviceType = metadata.DeviceType,
            AcceptLanguage = metadata.AcceptLanguage,
            Referer = metadata.Referer,
            Country = metadata.Country,
            City = metadata.City,

            TraceId = metadata.TraceId,
            CorrelationId = metadata.CorrelationId,
            RequestPath = metadata.RequestPath,
            RequestMethod = metadata.RequestMethod,

            EnvironmentName = metadata.EnvironmentName,
            MachineName = metadata.MachineName,
            ApplicationName = metadata.ApplicationName,
            ApplicationVersion = metadata.ApplicationVersion,
            ProcessId = metadata.ProcessId,
            ThreadId = metadata.ThreadId,

            EntityType = entry.Metadata.ClrType.Name,
            EntityId = entity.Id,
            Action = action,
            ChangesJson = JsonSerializer.Serialize(changes, JsonOptions),
        };
    }

    /// <summary>
    /// Aktif support session varsa Catalog ChangeTracker'a o session'ı yükler ve
    /// WriteActionCount'unu artırır. Bu değişiklik aynı SaveChanges içinde commit edilir.
    /// </summary>
    private static void IncrementSupportWriteCount(DbContext context, Guid supportSessionId)
    {
        var supportSession = context.Set<SupportSession>().Find(supportSessionId);
        if (supportSession is null)
        {
            return; // Aktif değil — sessizce geç (race condition).
        }

        // Tracked + Modified state'inde artır
        supportSession.WriteActionCount++;
    }

    /// <summary>Toplanmış entry'leri Dapper ile Audit DB'ye yazar.</summary>
    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO audit_entries (
                id, "timestamp",
                user_id, user_email, user_full_name,
                tenant_id, tenant_name, scope_level, company_id, unit_id, persona_side, roles_json,
                is_system_session, support_session_id, impersonated_by_user_id,
                ip_address, user_agent, browser_name, browser_version, operating_system, device_type,
                accept_language, referer, country, city,
                trace_id, correlation_id, request_path, request_method,
                environment_name, machine_name, application_name, application_version, process_id, thread_id,
                entity_type, entity_id, action, changes_json)
            VALUES (
                @Id, @Timestamp,
                @UserId, @UserEmail, @UserFullName,
                @TenantId, @TenantName, @ScopeLevel, @CompanyId, @UnitId, @PersonaSide, @RolesJson::jsonb,
                @IsSystemSession, @SupportSessionId, @ImpersonatedByUserId,
                @IpAddress, @UserAgent, @BrowserName, @BrowserVersion, @OperatingSystem, @DeviceType,
                @AcceptLanguage, @Referer, @Country, @City,
                @TraceId, @CorrelationId, @RequestPath, @RequestMethod,
                @EnvironmentName, @MachineName, @ApplicationName, @ApplicationVersion, @ProcessId, @ThreadId,
                @EntityType, @EntityId, @Action, @ChangesJson::jsonb);
            """;

        await using var conn = new NpgsqlConnection(_auditConnectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (var entry in _pendingEntries)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    sql,
                    new
                    {
                        entry.Id,
                        entry.Timestamp,
                        entry.UserId,
                        entry.UserEmail,
                        entry.UserFullName,
                        entry.TenantId,
                        entry.TenantName,
                        entry.ScopeLevel,
                        entry.CompanyId,
                        entry.UnitId,
                        entry.PersonaSide,
                        entry.RolesJson,
                        entry.IsSystemSession,
                        entry.SupportSessionId,
                        entry.ImpersonatedByUserId,
                        entry.IpAddress,
                        entry.UserAgent,
                        entry.BrowserName,
                        entry.BrowserVersion,
                        entry.OperatingSystem,
                        entry.DeviceType,
                        entry.AcceptLanguage,
                        entry.Referer,
                        entry.Country,
                        entry.City,
                        entry.TraceId,
                        entry.CorrelationId,
                        entry.RequestPath,
                        entry.RequestMethod,
                        entry.EnvironmentName,
                        entry.MachineName,
                        entry.ApplicationName,
                        entry.ApplicationVersion,
                        entry.ProcessId,
                        entry.ThreadId,
                        entry.EntityType,
                        entry.EntityId,
                        Action = (short)entry.Action,
                        entry.ChangesJson,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _pendingEntries.Clear();
        }
    }
}

/// <summary>
/// Application-side bağlamı: aktif support session id'si (Redis session'dan
/// çekilir). v0.1.7'de yalnız <c>SupportSessionId</c> taşır.
/// </summary>
internal sealed class ApplicationAuditState
{
    /// <summary>Aktif support session id'si (varsa).</summary>
    public Guid? SupportSessionId { get; set; }
}
