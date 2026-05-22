namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>Bütçe liste elemanı — GetBudgetsQuery dönüş tipi.</summary>
public record BudgetListItem(
    Guid Id,
    Guid AccountingPeriodId,
    Guid AccountCodeId,
    string AccountCode,
    string AccountName,
    Guid? CostCenterId,
    string? CostCenterName,
    decimal BudgetedAmount);
