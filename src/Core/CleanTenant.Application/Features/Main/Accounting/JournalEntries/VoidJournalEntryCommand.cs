using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// Kesinleşmiş (Posted) yevmiye fişini iptal eder.
/// <para>
/// VUK gereği iptal; orijinal fişi silmez, borç/alacak satırlarını
/// ters çeviren yeni bir ters fiş (Correction) oluşturur ve orijinal
/// fişi Voided durumuna taşır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.journal.void")]
public sealed record VoidJournalEntryCommand(
    Guid CompanyId,
    Guid TenantId,
    Guid EntryId,
    string VoidReason) : IRequest<Result<JournalEntryCreated>>;
