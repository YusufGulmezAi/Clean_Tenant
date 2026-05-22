using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// Tek bir bütçenin detayı: tüm versiyonları + kalem versiyonları.
/// </summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetDetailQuery(
    Guid CompanyId,
    Guid BudgetId) : IRequest<Result<BudgetDetail>>;

/// <summary>Bütçe detay özet'i.</summary>
public sealed record BudgetDetail(
    Guid Id,
    Guid CompanyId,
    Guid FiscalYearId,
    string FiscalYearLabel,
    string Title,
    string? Notes,
    BudgetStatus Status,
    Guid? CurrentVersionId,
    IReadOnlyList<BudgetVersionDto> Versions);

/// <summary>Bütçe versiyon özet'i + kalem sayısı.</summary>
public sealed record BudgetVersionDto(
    Guid Id,
    int VersionNumber,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy,
    string? RevisionReason,
    int LineCount,
    decimal TotalPlannedAmount);
