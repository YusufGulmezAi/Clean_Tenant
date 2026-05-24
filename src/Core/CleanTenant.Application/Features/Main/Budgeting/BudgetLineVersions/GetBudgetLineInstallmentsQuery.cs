using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Bir kalem versiyonunun mevcut taksit (Installment) satırlarını ay sırasına göre
/// listeler. Taksit ızgarası editörünü doldurmak için kullanılır.
/// </summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetLineInstallmentsQuery(
    Guid CompanyId,
    Guid BudgetLineVersionId) : IRequest<Result<IReadOnlyList<BudgetLineInstallmentDto>>>;

/// <summary>Tek bir taksit satırı (okuma).</summary>
public sealed record BudgetLineInstallmentDto(
    Guid Id,
    int InstallmentNumber,
    int Year,
    int Month,
    decimal Amount,
    string? Label,
    bool IsManuallyEdited);
