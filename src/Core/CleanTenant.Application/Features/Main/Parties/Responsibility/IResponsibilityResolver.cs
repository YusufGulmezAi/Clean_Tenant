using CleanTenant.Domain.Tenant.Parties.Enums;

namespace CleanTenant.Application.Features.Main.Parties.Responsibility;

/// <summary>
/// Bir tahakkuk döneminin (ay) borcunu, BB tenure'larına göre gün-bazlı taraflara
/// böler (proration). Tahakkuk üretimi (S3) ve reattribution bunu kullanır.
/// </summary>
public interface IResponsibilityResolver
{
    /// <summary>
    /// Verilen BB+tutar girdileri için, (year, month) ayı boyunca gün-bazlı
    /// sorumluluk parçalarını hesaplar. Tenure'lar tek seferde yüklenir (N+1 yok).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ResponsibilityResult>> ProrateBatchAsync(
        IReadOnlyCollection<UnitAccrualInput> units,
        int year,
        int month,
        ResponsibilityMode mode,
        CancellationToken cancellationToken);
}

/// <summary>Proration girdisi — bir BB ve o BB'nin dönemlik toplam borcu.</summary>
public sealed record UnitAccrualInput(Guid UnitId, decimal Amount);

/// <summary>Bir BB için proration sonucu.</summary>
public sealed record ResponsibilityResult(
    Guid? PrimaryPartyId,
    string Note,
    IReadOnlyList<ResponsibilitySplitDto> Splits);

/// <summary>Tek bir gün-bazlı sorumluluk parçası.</summary>
public sealed record ResponsibilitySplitDto(
    Guid PartyId,
    ResponsibilityKind Kind,
    DateOnly FromDate,
    DateOnly ToDate,
    int DayCount,
    decimal Amount);
