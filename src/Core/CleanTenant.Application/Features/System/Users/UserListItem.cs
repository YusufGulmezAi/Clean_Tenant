namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kullanıcı listesi satır verisi. Scope bağımsız; her scope için aynı DTO kullanılır.
/// </summary>
public record UserListItem(
    Guid Id,
    string UrlCode,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsLocked,
    bool TwoFactorEnabled,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> RoleNames)
{
    /// <summary>Görüntüleme adı (Ad Soyad).</summary>
    public string FullName => $"{FirstName} {LastName}";
}
