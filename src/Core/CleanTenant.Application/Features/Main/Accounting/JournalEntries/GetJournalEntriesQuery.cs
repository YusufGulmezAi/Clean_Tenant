using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Şirkete ait yevmiye fişlerini filtreli ve sayfalı listeler.
/// <para>
/// Dönem, durum, fiş tipi ve tarih aralığı bazında filtreleme desteklenir.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.read")]
public sealed record GetJournalEntriesQuery(
    Guid CompanyId,
    Guid? AccountingPeriodId,
    JournalEntryStatus? Status,
    EntryType? EntryType,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PagedResult<JournalEntryListItem>>>;
