using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Onay bekleyen yevmiye fişini reddeder (PendingApproval → Draft).
/// <para>
/// Reddedilen fiş tekrar düzenlenebilir ve onaya gönderilebilir.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.approve")]
public sealed record RejectJournalEntryCommand(
    Guid CompanyId,
    Guid EntryId,
    string? RejectionReason) : IRequest<Result>;
