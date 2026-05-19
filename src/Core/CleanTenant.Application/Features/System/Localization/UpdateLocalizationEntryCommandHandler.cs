using CleanTenant.Application.Common.Localization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Localization;

/// <summary>
/// <para>
/// <see cref="UpdateLocalizationEntryCommand"/> handler. Kayıt bulunur,
/// değer güncellenir, <c>IsMachineTranslated</c> false yapılır (admin manuel
/// çeviri yaptığı için), SaveChanges, cache reload.
/// </para>
/// <para>
/// Audit Interceptor <c>LocalizedResource</c> mutasyonunu otomatik yakalar;
/// burada ek audit kaydı gerekmez.
/// </para>
/// </summary>
public sealed class UpdateLocalizationEntryCommandHandler
    : IRequestHandler<UpdateLocalizationEntryCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly ILocalizationCacheRefresher _cacheRefresher;

    public UpdateLocalizationEntryCommandHandler(
        ICatalogDbContext db,
        ILocalizationCacheRefresher cacheRefresher)
    {
        _db = db;
        _cacheRefresher = cacheRefresher;
    }

    public async Task<Result> Handle(
        UpdateLocalizationEntryCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await _db.LocalizedResources
            .FirstOrDefaultAsync(
                r => r.Key == command.Key && r.Culture == command.Culture && !r.IsDeleted,
                cancellationToken);

        if (entry is null)
        {
            return Result.Failure(
                Error.NotFound("LOCALIZATION-ENTRY-NOT-FOUND",
                    $"Lokalizasyon kaydı bulunamadı (Key='{command.Key}', Culture='{command.Culture}')."));
        }

        if (string.Equals(entry.Value, command.NewValue, StringComparison.Ordinal)
            && !entry.IsMachineTranslated)
        {
            return Result.Success();
        }

        entry.Value = command.NewValue;
        entry.IsMachineTranslated = false;

        await _db.SaveChangesAsync(cancellationToken);
        await _cacheRefresher.RefreshAsync(cancellationToken);

        return Result.Success();
    }
}
