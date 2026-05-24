using CleanTenant.Application.Features.Main.Parties.CurrentAccount;
using CleanTenant.Infrastructure.Persistence.Main;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <see cref="ICurrentAccountReader"/> Dapper implementasyonu. Tahakkuk detayları
/// (borç) ile tahsilat dağıtımları (alacak) birleştirilip yürüyen bakiye/KPI üretir.
/// Cache'lenmez — her çağrıda taze veri.
/// </summary>
public sealed class CurrentAccountReader : ICurrentAccountReader
{
    private readonly IDbContextFactory<MainDbContext> _dbFactory;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CurrentAccountReader(IDbContextFactory<MainDbContext> dbFactory)
        => _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LedgerEntryRow>> GetLedgerAsync(
        Guid companyId, Guid unitId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            WITH movements AS (
                SELECT
                    make_date(a.year, a.month, 1)::timestamptz AS movement_date,
                    a.description AS description,
                    d.amount      AS debit,
                    0::numeric    AS credit,
                    d.primary_responsible_party_id AS party_id,
                    'Accrual'     AS source,
                    1             AS ord
                FROM accrual_details d
                JOIN accruals a ON a.id = d.accrual_id
                    AND a.company_id = @companyId AND a.is_deleted = false
                WHERE d.unit_id = @unitId AND d.is_deleted = false

                UNION ALL

                SELECT
                    c.payment_date::timestamptz AS movement_date,
                    COALESCE(NULLIF(c.description, ''), 'Tahsilat') AS description,
                    0::numeric                  AS debit,
                    al.allocated_amount         AS credit,
                    NULL::uuid                  AS party_id,
                    'Collection'                AS source,
                    2                           AS ord
                FROM collection_allocations al
                JOIN accrual_details d2 ON d2.id = al.accrual_detail_id
                    AND d2.unit_id = @unitId AND d2.is_deleted = false
                JOIN collections c ON c.id = al.collection_id
                    AND c.company_id = @companyId AND c.is_deleted = false
                WHERE al.is_deleted = false
            )
            SELECT
                m.movement_date AS Date,
                m.description   AS Description,
                m.debit         AS Debit,
                m.credit        AS Credit,
                SUM(m.debit - m.credit) OVER (
                    ORDER BY m.movement_date, m.ord
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS RunningBalance,
                m.source        AS Source,
                p.full_name     AS ResponsiblePartyName
            FROM movements m
            LEFT JOIN parties p ON p.id = m.party_id AND p.is_deleted = false
            ORDER BY m.movement_date, m.ord
            """;

        var rows = await conn.QueryAsync<LedgerEntryRow>(
            new CommandDefinition(sql, new { companyId, unitId }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<CurrentAccountKpi> GetKpiAsync(
        Guid companyId, Guid unitId, DateOnly today, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                COALESCE(SUM(d.amount), 0)                                   AS TotalAccrued,
                COALESCE(SUM(paid.paid), 0)                                  AS TotalCollected,
                COALESCE(SUM(d.amount), 0) - COALESCE(SUM(paid.paid), 0)     AS NetBalance,
                COALESCE(SUM(CASE WHEN d.due_date < @today
                                  THEN d.amount - COALESCE(paid.paid, 0) ELSE 0 END), 0) AS OverdueAmount
            FROM accrual_details d
            JOIN accruals a ON a.id = d.accrual_id
                AND a.company_id = @companyId AND a.is_deleted = false
            LEFT JOIN LATERAL (
                SELECT COALESCE(SUM(al.allocated_amount), 0) AS paid
                FROM collection_allocations al
                WHERE al.accrual_detail_id = d.id AND al.is_deleted = false
            ) paid ON true
            WHERE d.unit_id = @unitId AND d.is_deleted = false
            """;

        // Dapper (bu sürüm) DateOnly parametresini desteklemez → DateTime'a çevir.
        var todayDt = today.ToDateTime(TimeOnly.MinValue);
        var kpi = await conn.QuerySingleAsync<CurrentAccountKpi>(
            new CommandDefinition(sql, new { companyId, unitId, today = todayDt }, cancellationToken: ct));
        return kpi;
    }
}
