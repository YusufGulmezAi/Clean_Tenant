using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Taslak versiyondaki bir Installment kalem versiyonunun taksit ızgarasını
/// (ay + tutar) tümüyle değiştirir. Mevcut taksitler soft-delete edilip yenileri
/// yazılır. Yalnız <b>Draft</b> versiyonda izinlidir.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record SetBudgetLineInstallmentsCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetLineVersionId,
    IReadOnlyList<InstallmentInput> Installments) : IRequest<Result>;

/// <summary>Tek bir taksit girişi.</summary>
public sealed record InstallmentInput(int Year, int Month, decimal Amount, string? Label = null);
