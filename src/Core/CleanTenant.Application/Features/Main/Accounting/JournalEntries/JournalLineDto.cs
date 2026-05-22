namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Yevmiye satırı oluşturma isteği — CreateJournalEntryCommand içinde kullanılır.
/// </summary>
public record JournalLineRequest(
    Guid AccountCodeId,
    decimal Debit,
    decimal Credit,
    string? Description,
    Guid? CostCenterId,
    string? TaxCode,
    Guid? UnitId,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    decimal? ExchangeRate);

/// <summary>
/// Yevmiye satırı detay görünümü — JournalEntryDetail içinde kullanılır.
/// <para>
/// <see cref="AccountCodeValue"/> rapor sorgularında join maliyetini düşürmek
/// için denormalize edilmiş hesap kodu değerini taşır.
/// </para>
/// </summary>
public record JournalLineDetail(
    Guid Id,
    Guid AccountCodeId,
    string AccountCodeValue,
    decimal Debit,
    decimal Credit,
    string? Description,
    Guid? CostCenterId,
    string? TaxCode);
