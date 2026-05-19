using MediatR;

namespace CleanTenant.Application.Features.Auth.CurrentSession;

/// <summary>
/// Aktif oturum hakkında UI'nın ihtiyaç duyduğu temel bilgileri döner.
/// Blazor Server component'leri Redis'teki <c>Permissions</c> listesine doğrudan
/// erişemez (component init'inde ICurrentSessionAccessor null gelir) — bu sorgu
/// MediatR pipeline'ı üzerinden çalışıp SessionLoaderBehavior'ın doldurduğu
/// oturuma erişerek o boşluğu doldurur.
/// </summary>
public sealed record GetCurrentSessionInfoQuery : IRequest<CurrentSessionInfo>;

/// <summary>
/// PermissionPicker gibi UI bileşenlerinin tükettiği oturum projection'ı.
/// </summary>
/// <param name="IsAuthenticated">Aktif bir oturum var mı.</param>
/// <param name="IsSystem">ScopeLevel == System mi (UI filter bypass için).</param>
/// <param name="PermissionCodes">Oturumun sahip olduğu izin kodları (privilege ceiling kontrolü için).</param>
public sealed record CurrentSessionInfo(
    bool IsAuthenticated,
    bool IsSystem,
    IReadOnlyList<string> PermissionCodes);
