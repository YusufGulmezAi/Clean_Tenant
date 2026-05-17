using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanTenant.Infrastructure.Persistence.Interceptors;

/// <summary>
/// <para>
/// SaveChanges öncesi ChangeTracker entry'lerini tarayarak audit ve
/// soft-delete alanlarını otomatik dolduran EF Core interceptor'ı.
/// </para>
/// <para>
/// <b>Davranışlar:</b>
/// <list type="bullet">
///   <item><see cref="IEntity"/> Added: <c>Id</c> boşsa <see cref="Guid.CreateVersion7()"/> ile doldurulur (zaman-sıralı).</item>
///   <item><see cref="IAuditable"/> Added: <c>CreatedAt</c>, <c>CreatedBy</c>.</item>
///   <item><see cref="IAuditable"/> Modified: <c>UpdatedAt</c>, <c>UpdatedBy</c>.</item>
///   <item><see cref="ISoftDeletable"/> Deleted: state'i Modified'a çevirir, <c>IsDeleted=true</c>, <c>DeletedAt</c>, <c>DeletedBy</c> doldurur.</item>
/// </list>
/// </para>
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;
    private readonly IUserContext _userContext;

    /// <summary>Audit bağımlılıklarını alır.</summary>
    /// <param name="clock">Zaman kaynağı (testlerde mock'lanabilir).</param>
    /// <param name="userContext">Mevcut kullanıcı bağlamı; CreatedBy/UpdatedBy/DeletedBy için.</param>
    public AuditingInterceptor(IClock clock, IUserContext userContext)
    {
        _clock = clock;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditing(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditing(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    private void ApplyAuditing(DbContext context)
    {
        var now = _clock.UtcNow;
        var userId = _userContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // ID otomatik üretimi (UUID v7 — zaman-sıralı)
            if (entry.State == EntityState.Added && entry.Entity is IEntity entity && entity.Id == Guid.Empty)
            {
                entry.Property(nameof(IEntity.Id)).CurrentValue = Guid.CreateVersion7(now);
            }

            // Audit alanları
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = userId;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        auditable.UpdatedBy = userId;
                        break;
                }
            }

            // Soft delete dönüşümü
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = userId;
            }
        }
    }
}
