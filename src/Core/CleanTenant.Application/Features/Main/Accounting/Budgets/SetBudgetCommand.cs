using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// Bütçe kalemini oluşturur veya günceller (upsert).
/// <para>
/// (CompanyId + AccountingPeriodId + AccountCodeId + CostCenterId) kombinasyonu
/// benzersiz olmalıdır; mevcut kayıt varsa <see cref="BudgetedAmount"/> güncellenir,
/// yoksa yeni kayıt eklenir.
/// </para>
/// <para>
/// Yalnızca <c>IsDetail = true</c> olan yaprak hesaplara bütçe girilebilir.
/// </para>
/// </summary>
[RequirePermission("company.accounting.budget.write")]
public sealed record SetBudgetCommand(
    Guid CompanyId,
    Guid TenantId,
    Guid AccountingPeriodId,
    Guid AccountCodeId,
    Guid? CostCenterId,
    decimal BudgetedAmount) : IRequest<Result<Guid>>;
