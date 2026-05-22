using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.Readers;

// ---------------------------------------------------------------------------
// Rapor DTO'ları — IAccountingReader metodlarının dönüş tipleri
// Implementasyon Faz 6'da gelecek; şimdi sadece tip tanımları.
// ---------------------------------------------------------------------------

public record TrialBalanceReport(IReadOnlyList<TrialBalanceLine> Lines);

public record TrialBalanceLine(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    decimal Balance);

public record GeneralLedgerEntry(
    DateOnly EntryDate,
    string EntryNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

public record BalanceSheetReport(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    IReadOnlyList<BalanceSheetLine> Lines);

public record BalanceSheetLine(
    string AccountCode,
    string AccountName,
    AccountClass Class,
    decimal Balance);

public record IncomeStatementReport(
    decimal Revenue,
    decimal Expenses,
    decimal NetIncome,
    IReadOnlyList<IncomeStatementLine> Lines);

public record IncomeStatementLine(
    string AccountCode,
    string AccountName,
    decimal Amount);

public record AccountStatementEntry(
    DateOnly EntryDate,
    string EntryNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

/// <summary>
/// KDV özet raporu — aylık/çeyreklik beyanname taslağı.
/// </summary>
public record VatSummaryReport(
    decimal IndirilecekKdv,
    decimal HesaplananKdv,
    decimal Devreden,
    decimal Odenecek);

public record CostCenterReportEntry(
    string CostCenterCode,
    string CostCenterName,
    string AccountCode,
    string AccountName,
    decimal Amount);

public record BudgetVsActualReport(IReadOnlyList<BudgetVsActualLine> Lines);

public record BudgetVsActualLine(
    string AccountCode,
    string AccountName,
    decimal Budgeted,
    decimal Actual,
    decimal Variance);

public record CashBookEntry(
    DateOnly EntryDate,
    string EntryNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance);
