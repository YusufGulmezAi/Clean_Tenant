using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ExpenseCategories;

/// <summary>
/// Yeni gider kategorisi oluşturur. (CompanyId, Code) benzersizdir (BDG-201).
/// Parent kategori belirtilirse aynı şirkete ait olmalı (BDG-202).
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateExpenseCategoryCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid? ParentCategoryId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder) : IRequest<Result<Guid>>;
