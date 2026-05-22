using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Onay bekleyen yevmiye fişini onaylar ve muhasebeleştirir
/// (PendingApproval → Posted).
/// <para>
/// Dual-control kuralı: onaylayan kullanıcı fişi oluşturan kullanıcıdan
/// farklı olmalıdır (<c>ACC-403</c>).
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.approve")]
public sealed record ApproveJournalEntryCommand(
    Guid CompanyId,
    Guid EntryId) : IRequest<Result>;
