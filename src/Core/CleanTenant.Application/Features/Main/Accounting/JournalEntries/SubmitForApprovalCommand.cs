using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Yevmiye fişini onaya gönderir (Draft → PendingApproval).
/// <para>
/// Şirkette dual-control (RequireApproval) aktif olmalıdır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.write")]
public sealed record SubmitForApprovalCommand(
    Guid CompanyId,
    Guid EntryId) : IRequest<Result>;
