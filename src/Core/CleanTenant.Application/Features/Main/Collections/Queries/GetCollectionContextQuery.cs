using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary>
/// Tahsilat sihirbazı için bağlam: şirketin <b>açık</b> muhasebe dönemleri +
/// tahsilatta borç hesabı olarak seçilebilecek kasa/banka/çek hesapları (100/101/102).
/// <para>
/// Tenant kapsamlı (<c>tenant.collection.record</c>) — Cari Kart kullanıcısı
/// şirket muhasebe-plan izni olmadan da sihirbazı çalıştırabilsin diye
/// company-scoped <c>GetCurrentFiscalYearQuery</c>/<c>GetAccountCodesQuery</c> yerine bu kullanılır.
/// </para>
/// </summary>
[RequirePermission("tenant.collection.record")]
public sealed record GetCollectionContextQuery(
    Guid CompanyId) : IRequest<Result<CollectionContext>>;

/// <summary>Tahsilat sihirbazı bağlamı.</summary>
public sealed record CollectionContext(
    IReadOnlyList<OpenPeriodItem> OpenPeriods,
    IReadOnlyList<CashAccountItem> CashAccounts);

/// <summary>Açık muhasebe dönemi (tahsilat yevmiyesinin işleneceği dönem).</summary>
public sealed record OpenPeriodItem(
    Guid Id,
    int Year,
    int Month);

/// <summary>Tahsilatta borçlandırılacak kasa/banka/çek hesabı.</summary>
public sealed record CashAccountItem(
    Guid Id,
    string Code,
    string Name,
    CashAccountKind Kind);

/// <summary>Kasa/banka/çek hesap türü — sihirbazda kanala göre filtreler.</summary>
public enum CashAccountKind
{
    /// <summary>Kasa (100) — Nakit kanalı.</summary>
    Cash = 0,

    /// <summary>Alınan Çekler (101) — Çek kanalı.</summary>
    Check = 1,

    /// <summary>Bankalar (102) — POS kanalı.</summary>
    Bank = 2
}
