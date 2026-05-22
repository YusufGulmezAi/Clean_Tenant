using CleanTenant.Application.Features.Main.Accounting.Readers;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Infrastructure.Persistence.Main;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <see cref="IAccountingReader"/> Dapper implementasyonu.
/// Raporlar cache'lenmez — her çağrıda taze veri döner.
/// </summary>
public sealed class AccountingReader : IAccountingReader
{
    private readonly IDbContextFactory<MainDbContext> _dbFactory;

    /// <inheritdoc />
    public AccountingReader(IDbContextFactory<MainDbContext> dbFactory)
        => _dbFactory = dbFactory;

    // ─── Mizan ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<TrialBalanceReport> GetTrialBalanceAsync(
        Guid companyId, Guid fiscalYearId, int? month, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                jl.account_code AS AccountCode,
                COALESCE(ac.name, jl.account_code) AS AccountName,
                COALESCE(SUM(jl.debit), 0)  AS Debit,
                COALESCE(SUM(jl.credit), 0) AS Credit,
                COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) AS Balance
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
            JOIN accounting_periods ap ON ap.id = je.accounting_period_id
                AND ap.fiscal_year_id = @fiscalYearId
                AND ap.is_deleted = false
            LEFT JOIN account_codes ac ON ac.company_id = jl.company_id
                AND ac.code = jl.account_code
                AND ac.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.is_deleted = false
                AND (@month IS NULL OR ap.month = @month)
            GROUP BY jl.account_code, ac.name
            HAVING COALESCE(SUM(jl.debit), 0) <> 0
                OR COALESCE(SUM(jl.credit), 0) <> 0
            ORDER BY jl.account_code
            """;

        var rows = await conn.QueryAsync<TrialBalanceRow>(
            new CommandDefinition(sql, new { companyId, fiscalYearId, month }, cancellationToken: ct));

        var lines = rows.Select(r => new TrialBalanceLine(r.AccountCode, r.AccountName, r.Debit, r.Credit, r.Balance))
                        .ToList();
        return new TrialBalanceReport(lines);
    }

    // ─── Büyük Defter ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneralLedgerEntry>> GetGeneralLedgerAsync(
        Guid companyId, string accountCode, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                je.entry_date   AS EntryDate,
                je.entry_number AS EntryNumber,
                COALESCE(jl.description, je.description) AS Description,
                jl.debit        AS Debit,
                jl.credit       AS Credit,
                SUM(jl.debit - jl.credit) OVER (
                    ORDER BY je.entry_date, je.entry_number
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS RunningBalance
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date BETWEEN @from AND @to
            WHERE jl.company_id = @companyId
                AND jl.account_code = @accountCode
                AND jl.is_deleted = false
            ORDER BY je.entry_date, je.entry_number
            """;

        var rows = await conn.QueryAsync<LedgerRow>(
            new CommandDefinition(sql,
                new { companyId, accountCode, from = from.ToDateTime(TimeOnly.MinValue), to = to.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct));

        return rows.Select(r => new GeneralLedgerEntry(
                DateOnly.FromDateTime(r.EntryDate), r.EntryNumber, r.Description,
                r.Debit, r.Credit, r.RunningBalance))
            .ToList();
    }

    // ─── Bilanço ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<BalanceSheetReport> GetBalanceSheetAsync(
        Guid companyId, DateOnly asOf, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                jl.account_code AS AccountCode,
                COALESCE(ac.name, jl.account_code) AS AccountName,
                COALESCE(ac.account_class, 0) AS Class,
                COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) AS Balance
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date <= @asOf
            LEFT JOIN account_codes ac ON ac.company_id = jl.company_id
                AND ac.code = jl.account_code
                AND ac.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.is_deleted = false
                AND COALESCE(ac.account_class, 0) IN (1, 2, 3, 4, 5)
            GROUP BY jl.account_code, ac.name, ac.account_class
            ORDER BY jl.account_code
            """;

        var rows = (await conn.QueryAsync<BalanceSheetRow>(
            new CommandDefinition(sql,
                new { companyId, asOf = asOf.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct))).ToList();

        var lines = rows.Select(r => new BalanceSheetLine(
                r.AccountCode, r.AccountName, (AccountClass)r.Class, r.Balance))
            .ToList();

        var totalAssets      = rows.Where(r => r.Class is 1 or 2).Sum(r => r.Balance);
        var totalLiabilities = rows.Where(r => r.Class is 3 or 4).Sum(r => r.Balance);
        var totalEquity      = rows.Where(r => r.Class == 5).Sum(r => r.Balance);

        return new BalanceSheetReport(totalAssets, totalLiabilities, totalEquity, lines);
    }

    // ─── Gelir Tablosu ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IncomeStatementReport> GetIncomeStatementAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                jl.account_code AS AccountCode,
                COALESCE(ac.name, jl.account_code) AS AccountName,
                COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) AS Amount
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date BETWEEN @from AND @to
            LEFT JOIN account_codes ac ON ac.company_id = jl.company_id
                AND ac.code = jl.account_code
                AND ac.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.is_deleted = false
                AND COALESCE(ac.account_class, 0) = 6
            GROUP BY jl.account_code, ac.name
            ORDER BY jl.account_code
            """;

        var rows = (await conn.QueryAsync<SimpleAccountRow>(
            new CommandDefinition(sql,
                new { companyId, from = from.ToDateTime(TimeOnly.MinValue), to = to.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct))).ToList();

        var lines = rows.Select(r => new IncomeStatementLine(r.AccountCode, r.AccountName, r.Amount)).ToList();

        // 6xx alacak dominant hesaplar gelir (bakiye < 0), borç dominant gider
        var revenue   = lines.Where(l => l.Amount < 0).Sum(l => Math.Abs(l.Amount));
        var expenses  = lines.Where(l => l.Amount > 0).Sum(l => l.Amount);
        var netIncome = revenue - expenses;

        return new IncomeStatementReport(revenue, expenses, netIncome, lines);
    }

    // ─── Hesap Ekstresi ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountStatementEntry>> GetAccountStatementAsync(
        Guid companyId, string accountCode, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                je.entry_date   AS EntryDate,
                je.entry_number AS EntryNumber,
                COALESCE(jl.description, je.description) AS Description,
                jl.debit        AS Debit,
                jl.credit       AS Credit,
                SUM(jl.debit - jl.credit) OVER (
                    ORDER BY je.entry_date, je.entry_number
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS RunningBalance
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date BETWEEN @from AND @to
            WHERE jl.company_id = @companyId
                AND jl.account_code = @accountCode
                AND jl.is_deleted = false
            ORDER BY je.entry_date, je.entry_number
            """;

        var rows = await conn.QueryAsync<LedgerRow>(
            new CommandDefinition(sql,
                new { companyId, accountCode, from = from.ToDateTime(TimeOnly.MinValue), to = to.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct));

        return rows.Select(r => new AccountStatementEntry(
                DateOnly.FromDateTime(r.EntryDate), r.EntryNumber, r.Description,
                r.Debit, r.Credit, r.RunningBalance))
            .ToList();
    }

    // ─── KDV Özeti ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<VatSummaryReport> GetVatSummaryAsync(
        Guid companyId, int year, int month, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();
        var param = new { companyId, year, month };

        // İndirilecek KDV — 191.xx borç tarafı
        const string sql191 = """
            SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
            JOIN accounting_periods ap ON ap.id = je.accounting_period_id
                AND ap.year = @year AND ap.month = @month
                AND ap.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.account_code LIKE '191%'
                AND jl.is_deleted = false
            """;

        // Hesaplanan KDV — 391.xx alacak tarafı
        const string sql391 = """
            SELECT COALESCE(SUM(jl.credit - jl.debit), 0)
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
            JOIN accounting_periods ap ON ap.id = je.accounting_period_id
                AND ap.year = @year AND ap.month = @month
                AND ap.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.account_code LIKE '391%'
                AND jl.is_deleted = false
            """;

        // Devreden KDV — 190.xx bakiyesi
        const string sql190 = """
            SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
            JOIN accounting_periods ap ON ap.id = je.accounting_period_id
                AND ap.year = @year AND ap.month = @month
                AND ap.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.account_code LIKE '190%'
                AND jl.is_deleted = false
            """;

        var indirilecek = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql191, param, cancellationToken: ct));
        var hesaplanan  = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql391, param, cancellationToken: ct));
        var devreden    = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql190, param, cancellationToken: ct));

        var odenecek = Math.Max(0, hesaplanan - indirilecek - devreden);
        return new VatSummaryReport(indirilecek, hesaplanan, devreden, odenecek);
    }

    // ─── Maliyet Merkezi Raporu ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostCenterReportEntry>> GetCostCenterReportAsync(
        Guid companyId, Guid? costCenterId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                COALESCE(cc.code, '?')          AS CostCenterCode,
                COALESCE(cc.name, 'Tanımsız')   AS CostCenterName,
                jl.account_code                 AS AccountCode,
                COALESCE(ac.name, jl.account_code) AS AccountName,
                COALESCE(SUM(jl.debit), 0)      AS Amount
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date BETWEEN @from AND @to
            LEFT JOIN cost_centers cc ON cc.id = jl.cost_center_id
                AND cc.is_deleted = false
            LEFT JOIN account_codes ac ON ac.company_id = jl.company_id
                AND ac.code = jl.account_code
                AND ac.is_deleted = false
            WHERE jl.company_id = @companyId
                AND jl.is_deleted = false
                AND (@costCenterId IS NULL OR jl.cost_center_id = @costCenterId)
            GROUP BY cc.code, cc.name, jl.account_code, ac.name
            HAVING COALESCE(SUM(jl.debit), 0) > 0
            ORDER BY cc.code NULLS LAST, jl.account_code
            """;

        var rows = await conn.QueryAsync<CostCenterRow>(
            new CommandDefinition(sql,
                new { companyId, costCenterId, from = from.ToDateTime(TimeOnly.MinValue), to = to.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct));

        return rows.Select(r => new CostCenterReportEntry(
                r.CostCenterCode, r.CostCenterName, r.AccountCode, r.AccountName, r.Amount))
            .ToList();
    }

    // ─── Bütçe-Gerçekleşen Karşılaştırma ────────────────────────────────────

    /// <inheritdoc />
    public async Task<BudgetVsActualReport> GetBudgetVsActualAsync(
        Guid companyId, Guid fiscalYearId, int? month, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            WITH budget_agg AS (
                SELECT
                    ac.code AS account_code,
                    ac.name AS account_name,
                    SUM(b.budgeted_amount) AS budgeted
                FROM budgets b
                JOIN account_codes ac ON ac.id = b.account_code_id
                    AND ac.company_id = @companyId
                    AND ac.is_deleted = false
                JOIN accounting_periods ap ON ap.id = b.accounting_period_id
                    AND ap.fiscal_year_id = @fiscalYearId
                    AND ap.is_deleted = false
                    AND (@month IS NULL OR ap.month = @month)
                WHERE b.company_id = @companyId
                    AND b.is_deleted = false
                GROUP BY ac.code, ac.name
            ),
            actual_agg AS (
                SELECT
                    jl.account_code,
                    SUM(jl.debit) - SUM(jl.credit) AS actual
                FROM journal_lines jl
                JOIN journal_entries je ON je.id = jl.journal_entry_id
                    AND je.company_id = @companyId
                    AND je.status = 2
                    AND je.is_deleted = false
                JOIN accounting_periods ap ON ap.id = je.accounting_period_id
                    AND ap.fiscal_year_id = @fiscalYearId
                    AND ap.is_deleted = false
                    AND (@month IS NULL OR ap.month = @month)
                WHERE jl.company_id = @companyId
                    AND jl.is_deleted = false
                GROUP BY jl.account_code
            )
            SELECT
                COALESCE(ba.account_code, aa.account_code) AS AccountCode,
                COALESCE(ba.account_name, aa.account_code) AS AccountName,
                COALESCE(ba.budgeted, 0)                   AS Budgeted,
                COALESCE(aa.actual, 0)                     AS Actual,
                COALESCE(aa.actual, 0) - COALESCE(ba.budgeted, 0) AS Variance
            FROM budget_agg ba
            FULL OUTER JOIN actual_agg aa ON aa.account_code = ba.account_code
            ORDER BY AccountCode
            """;

        var rows = await conn.QueryAsync<BudgetVsActualRow>(
            new CommandDefinition(sql, new { companyId, fiscalYearId, month }, cancellationToken: ct));

        var lines = rows.Select(r => new BudgetVsActualLine(
                r.AccountCode, r.AccountName, r.Budgeted, r.Actual, r.Variance))
            .ToList();
        return new BudgetVsActualReport(lines);
    }

    // ─── Kasa/Banka Defteri ──────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<CashBookEntry>> GetCashBookAsync(
        Guid companyId, string accountCode, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                je.entry_date   AS EntryDate,
                je.entry_number AS EntryNumber,
                COALESCE(jl.description, je.description) AS Description,
                jl.debit        AS Debit,
                jl.credit       AS Credit,
                SUM(jl.debit - jl.credit) OVER (
                    ORDER BY je.entry_date, je.entry_number
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS Balance
            FROM journal_lines jl
            JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @companyId
                AND je.status = 2
                AND je.is_deleted = false
                AND je.entry_date BETWEEN @from AND @to
            WHERE jl.company_id = @companyId
                AND jl.account_code = @accountCode
                AND jl.is_deleted = false
            ORDER BY je.entry_date, je.entry_number
            """;

        var rows = await conn.QueryAsync<CashBookRow>(
            new CommandDefinition(sql,
                new { companyId, accountCode, from = from.ToDateTime(TimeOnly.MinValue), to = to.ToDateTime(TimeOnly.MinValue) },
                cancellationToken: ct));

        return rows.Select(r => new CashBookEntry(
                DateOnly.FromDateTime(r.EntryDate), r.EntryNumber, r.Description,
                r.Debit, r.Credit, r.Balance))
            .ToList();
    }

    // ─── Private Dapper row types ────────────────────────────────────────────

    private sealed class TrialBalanceRow
    {
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Debit   { get; set; }
        public decimal Credit  { get; set; }
        public decimal Balance { get; set; }
    }

    private sealed class LedgerRow
    {
        public DateTime EntryDate      { get; set; }
        public string   EntryNumber    { get; set; } = "";
        public string   Description    { get; set; } = "";
        public decimal  Debit          { get; set; }
        public decimal  Credit         { get; set; }
        public decimal  RunningBalance { get; set; }
    }

    private sealed class BalanceSheetRow
    {
        public string  AccountCode { get; set; } = "";
        public string  AccountName { get; set; } = "";
        public int     Class       { get; set; }
        public decimal Balance     { get; set; }
    }

    private sealed class SimpleAccountRow
    {
        public string  AccountCode { get; set; } = "";
        public string  AccountName { get; set; } = "";
        public decimal Amount      { get; set; }
    }

    private sealed class CostCenterRow
    {
        public string  CostCenterCode { get; set; } = "";
        public string  CostCenterName { get; set; } = "";
        public string  AccountCode    { get; set; } = "";
        public string  AccountName    { get; set; } = "";
        public decimal Amount         { get; set; }
    }

    private sealed class BudgetVsActualRow
    {
        public string  AccountCode { get; set; } = "";
        public string  AccountName { get; set; } = "";
        public decimal Budgeted    { get; set; }
        public decimal Actual      { get; set; }
        public decimal Variance    { get; set; }
    }

    private sealed class CashBookRow
    {
        public DateTime EntryDate   { get; set; }
        public string   EntryNumber { get; set; } = "";
        public string   Description { get; set; } = "";
        public decimal  Debit       { get; set; }
        public decimal  Credit      { get; set; }
        public decimal  Balance     { get; set; }
    }
}
