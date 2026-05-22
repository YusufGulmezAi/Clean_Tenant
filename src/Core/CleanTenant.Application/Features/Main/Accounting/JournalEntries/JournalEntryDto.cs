using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Yevmiye fişi liste elemanı — GetJournalEntriesQuery dönüş tipi.
/// </summary>
public record JournalEntryListItem(
    Guid Id,
    string EntryNumber,
    DateOnly EntryDate,
    EntryType EntryType,
    string Description,
    decimal TotalDebit,
    decimal TotalCredit,
    JournalEntryStatus Status);

/// <summary>
/// Yevmiye fişi tam detay — GetJournalEntryDetailQuery dönüş tipi.
/// <para><see cref="Lines"/> tüm borç/alacak satırlarını içerir.</para>
/// </summary>
public record JournalEntryDetail(
    Guid Id,
    string EntryNumber,
    DateOnly EntryDate,
    EntryType EntryType,
    string Description,
    string? Reference,
    Guid? ReferenceId,
    decimal TotalDebit,
    decimal TotalCredit,
    JournalEntryStatus Status,
    DateTimeOffset? PostedAt,
    Guid? PostedBy,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedBy,
    string? VoidReason,
    Guid? OriginalEntryId,
    IReadOnlyList<JournalLineDetail> Lines);

/// <summary>
/// Yevmiye fişi oluşturma komutunun başarı dönüşü.
/// </summary>
public record JournalEntryCreated(Guid Id, string EntryNumber);

/// <summary>
/// Sayfalı liste sonucu.
/// <para>
/// Projede genel bir PagedResult yoksa bu tanım kullanılır;
/// ileride merkezi bir tip oluşturulursa burası kaldırılır.
/// </para>
/// </summary>
/// <typeparam name="T">Liste eleman tipi.</typeparam>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
