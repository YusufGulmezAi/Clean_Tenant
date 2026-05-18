using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// <see cref="UpdateTenantCommand"/> handler. Yetki + immutable field kontrolü:
/// </para>
/// <list type="bullet">
///   <item>Sistem scope → her şey değişebilir.</item>
///   <item>TenantAdmin (session.TenantId == command.TenantId) → kimlik / BillingTier /
///   HasDedicatedDatabase değişimi reddedilir (immutable).</item>
///   <item>Diğer scope (Company/Unit/farklı Tenant) → 403.</item>
/// </list>
/// </summary>
public sealed class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly ICacheInvalidator _cacheInvalidator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateTenantCommandHandler(
        ICatalogDbContext db,
        ICurrentSessionAccessor sessionAccessor,
        ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null. Endpoint korumalı olmalı.");

        // Yetki: System veya TenantAdmin self
        var isSystem = session.ScopeLevel == ScopeLevel.System;
        var isTenantAdminSelf =
            session.ScopeLevel == ScopeLevel.Tenant
            && session.TenantId == command.TenantId
            && session.Roles.Contains("TenantAdmin", StringComparer.Ordinal);

        if (!isSystem && !isTenantAdminSelf)
        {
            return Result.Failure(
                Error.Forbidden("AUTH-FORBIDDEN", "Bu Yönetim'i düzenleme yetkiniz yok."));
        }

        var tenant = await _db.Tenants
            .Where(t => t.Id == command.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(
                Error.NotFound("TENANT-NOT-FOUND", "Yönetim bulunamadı."));
        }

        // TenantAdmin self update: immutable field'lar değişemez
        if (!isSystem)
        {
            if (tenant.LegalIdentityType != command.LegalIdentityType
                || tenant.LegalIdentityNumber != command.LegalIdentityNumber)
            {
                return Result.Failure(
                    Error.Forbidden("TENANT-LEGAL-ID-IMMUTABLE",
                        "Yönetim kimlik bilgisi yalnız Sistem operatörü tarafından değiştirilebilir."));
            }
            if (tenant.BillingTier != command.BillingTier)
            {
                return Result.Failure(
                    Error.Forbidden("TENANT-BILLING-IMMUTABLE",
                        "Faturalama katmanı yalnız Sistem operatörü tarafından değiştirilebilir."));
            }
            if (tenant.HasDedicatedDatabase != command.HasDedicatedDatabase)
            {
                return Result.Failure(
                    Error.Forbidden("TENANT-DEDICATED-DB-IMMUTABLE",
                        "Dedicated DB ayarı yalnız Sistem operatörü tarafından değiştirilebilir."));
            }
        }

        // Ad değişiyorsa tekillik kontrolü
        if (!string.Equals(tenant.Name, command.Name, StringComparison.Ordinal))
        {
            var nameTaken = await _db.Tenants.AsNoTracking()
                .AnyAsync(t => t.Id != command.TenantId
                            && t.Name == command.Name
                            && !t.IsDeleted, cancellationToken);
            if (nameTaken)
            {
                return Result.Failure(
                    Error.Conflict("TENANT-NAME-EXISTS",
                        $"'{command.Name}' adında başka bir Yönetim zaten kayıtlı."));
            }
        }

        // Kimlik numarası değişiyorsa (System scope) tekillik kontrolü
        if (isSystem
            && (tenant.LegalIdentityType != command.LegalIdentityType
                || tenant.LegalIdentityNumber != command.LegalIdentityNumber))
        {
            var idTaken = await _db.Tenants.AsNoTracking()
                .AnyAsync(t => t.Id != command.TenantId
                            && t.LegalIdentityNumber == command.LegalIdentityNumber
                            && !t.IsDeleted, cancellationToken);
            if (idTaken)
            {
                return Result.Failure(
                    Error.Conflict("TENANT-LEGAL-ID-EXISTS",
                        "Bu kimlik numarasıyla başka bir Yönetim kayıtlı."));
            }
        }

        // Mutate
        tenant.Name = command.Name;
        tenant.LegalName = command.LegalName;
        tenant.Address = command.Address;
        tenant.AllowSystemWriteAccess = command.AllowSystemWriteAccess;

        if (isSystem)
        {
            tenant.LegalIdentityType = command.LegalIdentityType;
            tenant.LegalIdentityNumber = command.LegalIdentityNumber;
            tenant.BillingTier = command.BillingTier;
            tenant.HasDedicatedDatabase = command.HasDedicatedDatabase;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Cache invalidate — by-id + list + global companies (TenantName denormalize)
        await _cacheInvalidator.InvalidateTenantAsync(command.TenantId, cancellationToken);

        return Result.Success();
    }
}
