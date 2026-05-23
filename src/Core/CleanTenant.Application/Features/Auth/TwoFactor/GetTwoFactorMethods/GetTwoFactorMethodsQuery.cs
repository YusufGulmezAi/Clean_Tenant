using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;

/// <summary>Authenticated kullanıcının 2FA durumu ve aktif yöntem listesi isteği.</summary>
public sealed record GetTwoFactorMethodsQuery : IRequest<Result<GetTwoFactorMethodsResult>>;

/// <summary>2FA durum özeti (profil güvenlik sekmesi için).</summary>
/// <param name="TwoFactorEnabled"><c>User.TwoFactorEnabled</c> bayrağı.</param>
/// <param name="AvailableMethods">Doğrulanmış (kullanılabilir) provider isimleri.</param>
/// <param name="RecoveryCodesLeft">Geriye kalan tek kullanımlık recovery code sayısı.</param>
/// <param name="AuthenticatorEnrolled">TOTP authenticator kuruldu mu.</param>
/// <param name="EmailConfirmed">E-posta 2FA yöntemi olarak doğrulandı mı.</param>
/// <param name="PhoneConfirmed">Telefon 2FA yöntemi olarak doğrulandı mı.</param>
/// <param name="Email">Hesaptaki e-posta adresi (gösterim için, yoksa null).</param>
/// <param name="PhoneNumber">Hesaptaki telefon numarası (gösterim için, yoksa null).</param>
/// <param name="IsSystemScope">Kullanıcı System scope'ta mı (2FA zorunlu, kapatılamaz).</param>
public sealed record GetTwoFactorMethodsResult(
    bool TwoFactorEnabled,
    IReadOnlyList<string> AvailableMethods,
    int RecoveryCodesLeft,
    bool AuthenticatorEnrolled,
    bool EmailConfirmed,
    bool PhoneConfirmed,
    string? Email,
    string? PhoneNumber,
    bool IsSystemScope);
