namespace CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;

/// <summary>Authenticated kullanıcının 2FA durumu ve aktif yöntem listesi isteği.</summary>
public sealed record GetTwoFactorMethodsQuery();

/// <summary>2FA durum özeti.</summary>
/// <param name="TwoFactorEnabled"><c>User.TwoFactorEnabled</c> bayrağı.</param>
/// <param name="AvailableMethods">Aktif provider isimleri (<c>"Authenticator"</c>, <c>"Email"</c>, <c>"Phone"</c>).</param>
/// <param name="RecoveryCodesLeft">Geriye kalan tek kullanımlık recovery code sayısı.</param>
public sealed record GetTwoFactorMethodsResult(
    bool TwoFactorEnabled,
    IReadOnlyList<string> AvailableMethods,
    int RecoveryCodesLeft);
