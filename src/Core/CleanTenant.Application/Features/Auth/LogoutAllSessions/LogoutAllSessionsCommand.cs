namespace CleanTenant.Application.Features.Auth.LogoutAllSessions;

/// <summary>
/// "Kendi tüm cihazlarımdan çıkış" isteği. Mevcut kullanıcının tüm aktif
/// Redis session'ları silinir; tüm refresh token zincirleri revoke edilir.
/// </summary>
public sealed record LogoutAllSessionsCommand();
