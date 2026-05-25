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

        // Banka-ekstresi modeli: tahakkuk (borç) + tahsilat TAM tutar (alacak) + iade (borç).
        // Tahsilat tam tutarla kredilenir → fazla ödeme bakiyeyi negatife (avans) çeker;
        // mahsup içsel olduğu için ayrı satır yok (tam tahsilat zaten kapsar); iade nakit
        // çıkışı olarak borç gösterilir. Yürüyen bakiye = BB'nin net borcu (negatif = avans).
        const string sql = """
            WITH movements AS (
                SELECT
                    make_date(a.year, a.month, 1)::timestamptz AS movement_date,
                    a.description AS description,
                    -- Düzeltme (negatif tutar) alacak tarafında gösterilir
                    CASE WHEN d.amount >= 0 THEN d.amount ELSE 0 END AS debit,
                    CASE WHEN d.amount < 0 THEN -d.amount ELSE 0 END AS credit,
                    d.primary_responsible_party_id AS party_id,
                    CASE WHEN a.source = 4 THEN 'Correction' ELSE 'Accrual' END AS source,
                    1             AS ord
                FROM accrual_details d
                JOIN accruals a ON a.id = d.accrual_id
                    AND a.company_id = @companyId AND a.is_deleted = false
                WHERE d.unit_id = @unitId AND d.is_deleted = false

                UNION ALL

                -- Tahsilat: tam tutar alacak (fazla ödeme bakiyeyi avansa/negatife çeker)
                SELECT
                    c.payment_date::timestamptz AS movement_date,
                    COALESCE(NULLIF(c.description, ''), 'Tahsilat') AS description,
                    0::numeric                  AS debit,
                    c.amount                    AS credit,
                    NULL::uuid                  AS party_id,
                    'Collection'                AS source,
                    2                           AS ord
                FROM collections c
                WHERE c.unit_id = @unitId AND c.company_id = @companyId AND c.is_deleted = false

                UNION ALL

                -- Avans iadesi: nakit çıkış → borç hareketi (avans/kredi bakiyesini azaltır)
                SELECT
                    r.refund_date::timestamptz AS movement_date,
                    'Avans iadesi'             AS description,
                    r.amount                   AS debit,
                    0::numeric                 AS credit,
                    NULL::uuid                 AS party_id,
                    'Refund'                   AS source,
                    3                          AS ord
                FROM collection_refunds r
                WHERE r.unit_id = @unitId AND r.company_id = @companyId AND r.is_deleted = false
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

        // TotalCollected = BB'ye yapılan TÜM tahsilat nakdi (collections.amount) — yani
        // dağıtılan + avans (unallocated). Böylece fazla ödemede NetBalance negatif
        // (alacaklı/avans) çıkar. Overdue, detay-bazlı ödenen (allocation) ile hesaplanır.
        const string sql = """
            SELECT
                acc.total_accrued                          AS TotalAccrued,
                coll.total_collected                       AS TotalCollected,
                acc.total_accrued - coll.total_collected   AS NetBalance,
                acc.overdue                                AS OverdueAmount,
                coll.advance_balance                       AS AdvanceBalance
            FROM
                (SELECT
                     COALESCE(SUM(d.amount), 0) AS total_accrued,
                     COALESCE(SUM(CASE WHEN d.due_date < @today
                                       THEN d.amount - COALESCE(paid.paid, 0) ELSE 0 END), 0) AS overdue
                 FROM accrual_details d
                 JOIN accruals a ON a.id = d.accrual_id
                     AND a.company_id = @companyId AND a.is_deleted = false
                 LEFT JOIN LATERAL (
                     SELECT COALESCE(SUM(al.allocated_amount), 0) AS paid
                     FROM collection_allocations al
                     WHERE al.accrual_detail_id = d.id AND al.is_deleted = false
                 ) paid ON true
                 WHERE d.unit_id = @unitId AND d.is_deleted = false) acc
                CROSS JOIN
                (SELECT COALESCE(SUM(c.amount), 0)
                        - COALESCE((SELECT SUM(r.amount) FROM collection_refunds r
                                    WHERE r.unit_id = @unitId AND r.company_id = @companyId
                                      AND r.is_deleted = false), 0) AS total_collected,
                        COALESCE(SUM(c.unallocated_amount), 0) AS advance_balance
                 FROM collections c
                 WHERE c.unit_id = @unitId AND c.company_id = @companyId AND c.is_deleted = false) coll
            """;

        // Dapper (bu sürüm) DateOnly parametresini desteklemez → DateTime'a çevir.
        var todayDt = today.ToDateTime(TimeOnly.MinValue);
        var kpi = await conn.QuerySingleAsync<CurrentAccountKpi>(
            new CommandDefinition(sql, new { companyId, unitId, today = todayDt }, cancellationToken: ct));
        return kpi;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitOverviewRow>> GetUnitsOverviewAsync(
        Guid companyId, DateOnly today, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();

        const string sql = """
            SELECT
                u.id     AS UnitId,
                u.number AS Number,
                b.name   AS BuildingName,
                COALESCE(SUM(d.amount), 0) - COALESCE(SUM(pa.paid), 0)
                    - COALESCE((SELECT SUM(c.unallocated_amount) FROM collections c
                                WHERE c.unit_id = u.id AND c.company_id = @companyId
                                  AND c.is_deleted = false), 0) AS RemainingBalance,
                COALESCE(SUM(CASE WHEN d.due_date < @today
                                  THEN d.amount - COALESCE(pa.paid, 0) ELSE 0 END), 0) AS OverdueAmount
            FROM units u
            JOIN buildings b ON b.id = u.building_id AND b.is_deleted = false
            JOIN parcels p   ON p.id = b.parcel_id AND p.is_deleted = false
            JOIN lands l     ON l.id = p.land_id AND l.is_deleted = false AND l.company_id = @companyId
            LEFT JOIN accrual_details d ON d.unit_id = u.id AND d.is_deleted = false
            LEFT JOIN accruals a ON a.id = d.accrual_id AND a.is_deleted = false
            LEFT JOIN LATERAL (
                SELECT COALESCE(SUM(al.allocated_amount), 0) AS paid
                FROM collection_allocations al
                WHERE al.accrual_detail_id = d.id AND al.is_deleted = false
            ) pa ON true
            WHERE u.is_deleted = false
            GROUP BY u.id, u.number, b.name, u.sort_order
            ORDER BY u.sort_order, u.number
            """;

        var todayDt = today.ToDateTime(TimeOnly.MinValue);
        var rows = await conn.QueryAsync<UnitOverviewRow>(
            new CommandDefinition(sql, new { companyId, today = todayDt }, cancellationToken: ct));
        return rows.ToList();
    }
}
