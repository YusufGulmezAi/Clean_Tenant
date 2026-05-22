using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLines;

/// <summary>
/// Yeni bütçe kalemi tanımı oluşturur. (CompanyId, Code) benzersizdir (BDG-301).
/// AccountCodeId verilirse TDHP yaprak hesabı olmalı.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateBudgetLineCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid ExpenseCategoryId,
    Guid? AccountCodeId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder = 0) : IRequest<Result<Guid>>;
