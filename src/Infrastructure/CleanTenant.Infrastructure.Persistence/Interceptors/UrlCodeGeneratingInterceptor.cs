using CleanTenant.Infrastructure.Persistence.Identifiers;
using CleanTenant.SharedKernel.Entities;
using CleanTenant.SharedKernel.Identifiers;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanTenant.Infrastructure.Persistence.Interceptors;

/// <summary>
/// <para>
/// <see cref="IHasUrlCode"/> implement eden Added entity'lerde <c>UrlCode</c>
/// boşsa otomatik üretim yapar ve <see cref="UrlCodeRegistry"/> tablosuna
/// kayıt ekler (global çarpışma kontrolü için).
/// </para>
/// <para>
/// <b>Çarpışma kontrolü:</b> Async yolda her üretilen kod için
/// <c>UrlCodeRegistry</c>'ye eşzamanlı (async) bakılır. Çakışma varsa yeniden
/// üretilir (en fazla 5 deneme). 9 karakter Base58 → 1.85×10¹⁵ kombinasyon;
/// pratikte retry asla çalışmaz, ancak DB unique constraint son güvence olarak
/// her durumda devrede kalır.
/// </para>
/// <para>
/// <b>Sync yolda</b> in-memory check yapılmaz; yalnız generate + tracked
/// entry eklenir. Çakışma çok düşük olasılığa karşı DB constraint'i fırlatır;
/// upper layer retry yapar.
/// </para>
/// </summary>
public sealed class UrlCodeGeneratingInterceptor : SaveChangesInterceptor
{
    private const int MaxRetryAttempts = 5;

    private readonly IUrlCodeGenerator _generator;
    private readonly IClock _clock;

    /// <summary>Kod üretici ve zaman kaynağını alır.</summary>
    public UrlCodeGeneratingInterceptor(IUrlCodeGenerator generator, IClock clock)
    {
        _generator = generator;
        _clock = clock;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await ApplyUrlCodesAsync(eventData.Context, cancellationToken);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyUrlCodes(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    private async Task ApplyUrlCodesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entries = GetIHasUrlCodeAddedEntries(context);
        if (entries.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        var registrySet = context.Set<UrlCodeRegistry>();

        foreach (var entry in entries)
        {
            var entity = (IHasUrlCode)entry.Entity;
            if (!string.IsNullOrEmpty(entity.UrlCode))
            {
                continue;
            }

            // Çarpışma kontrolü + en fazla 5 yeniden üretim
            string code = string.Empty;
            for (var attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                code = _generator.Generate();
                var exists = await registrySet.AsNoTracking()
                    .AnyAsync(r => r.Code == code, cancellationToken);
                if (!exists)
                {
                    break;
                }
            }

            entry.Property(nameof(IHasUrlCode.UrlCode)).CurrentValue = code;
            await registrySet.AddAsync(new UrlCodeRegistry
            {
                Code = code,
                OwnerType = entry.Entity.GetType().Name,
                OwnerId = GetEntityId(entry),
                CreatedAt = now,
            }, cancellationToken);
        }
    }

    private void ApplyUrlCodes(DbContext context)
    {
        var entries = GetIHasUrlCodeAddedEntries(context);
        if (entries.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        var registrySet = context.Set<UrlCodeRegistry>();

        foreach (var entry in entries)
        {
            var entity = (IHasUrlCode)entry.Entity;
            if (!string.IsNullOrEmpty(entity.UrlCode))
            {
                continue;
            }

            var code = _generator.Generate();
            entry.Property(nameof(IHasUrlCode.UrlCode)).CurrentValue = code;
            registrySet.Add(new UrlCodeRegistry
            {
                Code = code,
                OwnerType = entry.Entity.GetType().Name,
                OwnerId = GetEntityId(entry),
                CreatedAt = now,
            });
        }
    }

    private static List<EntityEntry> GetIHasUrlCodeAddedEntries(DbContext context)
    {
        // ToList() ile snapshot al; aksi halde aşağıdaki Add çağrıları ChangeTracker'ı modifiye eder.
        return [.. context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is IHasUrlCode)];
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        // AuditingInterceptor önce Id atamış olmalı (UUID v7); buradan okunur.
        var idProp = entry.Property(nameof(IEntity.Id));
        return (Guid)idProp.CurrentValue!;
    }
}
