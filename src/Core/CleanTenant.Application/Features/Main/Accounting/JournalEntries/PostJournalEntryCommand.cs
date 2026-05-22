using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Taslak yevmiye fişini muhasebeleştirir (Draft → Posted).
/// <para>
/// Yalnızca dual-control kapalı şirketlerde kullanılır.
/// Dual-control aktifse önce <see cref="SubmitForApprovalCommand"/> +
/// <see cref="ApproveJournalEntryCommand"/> akışı uygulanmalıdır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.post")]
public sealed record PostJournalEntryCommand(
    Guid CompanyId,
    Guid EntryId) : IRequest<Result>;
