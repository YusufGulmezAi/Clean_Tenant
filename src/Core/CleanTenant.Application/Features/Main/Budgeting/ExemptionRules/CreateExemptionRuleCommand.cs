using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ExemptionRules;

/// <summary>
/// Belirli BB için belirli bütçe kaleminden tarih aralıklı muafiyet tanımlar.
/// KMK m.18 paylaşım kuralları gereği gerekçe (<see cref="Reason"/>) zorunludur.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateExemptionRuleCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    Guid BudgetLineId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string Reason) : IRequest<Result<Guid>>;
