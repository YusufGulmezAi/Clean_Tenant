using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Yeni bir yevmiye fişi oluşturur (Draft durumunda).
/// <para>
/// İş kuralları handler'da uygulanır:
/// en az 2 satır, borç-alacak dengesi, hesap kodu geçerliliği vb.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.write")]
public sealed record CreateJournalEntryCommand(
    Guid CompanyId,
    Guid TenantId,
    Guid AccountingPeriodId,
    EntryType EntryType,
    DateOnly EntryDate,
    string Description,
    string? Reference,
    Guid? ReferenceId,
    IReadOnlyList<JournalLineRequest> Lines) : IRequest<Result<JournalEntryCreated>>;
