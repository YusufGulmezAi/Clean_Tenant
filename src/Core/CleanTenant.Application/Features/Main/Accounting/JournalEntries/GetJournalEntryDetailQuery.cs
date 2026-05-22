using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Tek bir yevmiye fişinin tüm satırlarıyla birlikte detayını getirir.
/// </summary>
[RequirePermission("company.accounting.journal.read")]
public sealed record GetJournalEntryDetailQuery(
    Guid CompanyId,
    Guid EntryId) : IRequest<Result<JournalEntryDetail>>;
