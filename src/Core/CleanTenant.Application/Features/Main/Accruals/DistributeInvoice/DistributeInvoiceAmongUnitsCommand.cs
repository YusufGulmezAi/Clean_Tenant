using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.DistributeInvoice;

/// <summary>
/// <para>
/// Bütçe dışı bir faturayı (örn. doğalgaz/su — sıcak su gideri) BB'lere dağıtarak
/// tahakkuk üretir. Bütçeye bağlı değildir; muhasebeci tutarı, dağıtım modelini,
/// katılım grubunu ve hesap kodlarını (120/600 alt hesapları) doğrudan belirler.
/// </para>
/// <para>
/// Tek bir <c>Accrual</c> (Source = Invoice) + BB başına <c>AccrualDetail</c> yazar
/// ve otomatik yevmiye fişi açar (<c>IAccrualJournalPoster</c>).
/// </para>
/// <para>
/// İdempotency: <see cref="InvoiceId"/> verildiyse aynı faturaya ikinci kez dağıtım
/// engellenir (ACR-405).
/// </para>
/// </summary>
[RequirePermission("tenant.accrual.generate")]
public sealed record DistributeInvoiceAmongUnitsCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid AccountingPeriodId,
    int Year,
    int Month,
    decimal TotalAmount,
    DistributionModel DistributionModel,
    Guid? ParticipationGroupId,
    Guid ReceivableAccountCodeId,
    Guid IncomeAccountCodeId,
    DateOnly DueDate,
    string Description,
    Guid? InvoiceId = null) : IRequest<Result<AccrualResult>>;
