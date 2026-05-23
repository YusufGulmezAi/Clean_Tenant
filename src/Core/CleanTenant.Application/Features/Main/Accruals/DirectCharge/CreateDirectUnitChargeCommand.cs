using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.DirectCharge;

/// <summary>
/// <para>
/// Tek bir Bağımsız Bölüme doğrudan (dağıtımsız) borç tahakkuk ettirir. Örnek:
/// depo/otopark kira, site yönetiminden ürün satışı. Bütçeye bağlı değildir.
/// </para>
/// <para>
/// Tek satırlı <c>Accrual</c> (Source = DirectCharge) + tek <c>AccrualDetail</c>
/// yazar ve otomatik yevmiye fişi açar. Hesap kodlarını (120/600 alt hesapları)
/// muhasebeci belirler.
/// </para>
/// </summary>
[RequirePermission("tenant.accrual.generate")]
public sealed record CreateDirectUnitChargeCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid AccountingPeriodId,
    Guid UnitId,
    int Year,
    int Month,
    decimal Amount,
    Guid ReceivableAccountCodeId,
    Guid IncomeAccountCodeId,
    DateOnly DueDate,
    string Description) : IRequest<Result<AccrualResult>>;
