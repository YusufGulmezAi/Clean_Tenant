namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary>
/// BB-merkezli cari hesap okuma katmanı (Dapper). Tahakkuk (borç) + tahsilat
/// (alacak) hareketlerini birleşik defter + KPI olarak döndürür.
/// </summary>
public interface ICurrentAccountReader
{
    /// <summary>Bir BB'nin cari hareket defteri (tarih sıralı + yürüyen bakiye).</summary>
    Task<IReadOnlyList<LedgerEntryRow>> GetLedgerAsync(Guid companyId, Guid unitId, CancellationToken ct);

    /// <summary>Bir BB'nin cari KPI özeti (tahakkuk/tahsilat/bakiye/vadesi geçmiş).</summary>
    Task<CurrentAccountKpi> GetKpiAsync(Guid companyId, Guid unitId, DateOnly today, CancellationToken ct);

    /// <summary>Şirketin tüm BB'leri + borç özeti (BB listesi / genel bakış).</summary>
    Task<IReadOnlyList<UnitOverviewRow>> GetUnitsOverviewAsync(Guid companyId, DateOnly today, CancellationToken ct);
}

/// <summary>Cari hareket satırı (borç/alacak/bakiye).</summary>
public sealed record LedgerEntryRow(
    DateTime Date,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Source,
    string? ResponsiblePartyName);

/// <summary>BB cari KPI özeti.</summary>
public sealed record CurrentAccountKpi(
    decimal TotalAccrued,
    decimal TotalCollected,
    decimal NetBalance,
    decimal OverdueAmount,
    decimal AdvanceBalance);

/// <summary>BB listesi satırı (borç özetli).</summary>
public sealed record UnitOverviewRow(
    Guid UnitId,
    string Number,
    string BuildingName,
    decimal RemainingBalance,
    decimal OverdueAmount);
