namespace CleanTenant.Application.Features.Auth.TwoFactor.RegenerateRecoveryCodes;

/// <summary>
/// Authenticated kullanıcının kendi recovery code'larını yeniden üret isteği.
/// Eski tüm kodlar invalidate edilir; istemciye yeni 10 kod döner.
/// </summary>
public sealed record RegenerateRecoveryCodesCommand();

/// <summary>10 adet yeni recovery code; bir kere döner.</summary>
public sealed record RegenerateRecoveryCodesResult(IReadOnlyList<string> RecoveryCodes);
