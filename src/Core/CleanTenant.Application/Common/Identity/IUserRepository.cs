using CleanTenant.Domain.Identity.Users;

namespace CleanTenant.Application.Common.Identity;

/// <summary>
/// UserManager operasyonlarını saran soyutlama. Application katmanının
/// ASP.NET Core Identity'e doğrudan bağımlı olmasını engeller.
/// </summary>
public interface IUserRepository
{
    /// <summary>E-posta adresi başka bir kullanıcıda kayıtlı mı kontrol eder.</summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken ct = default);

    /// <summary>Yeni kullanıcı oluşturur ve şifresini hash'ler.</summary>
    Task<IdentityOperationResult> CreateAsync(User user, string password, CancellationToken ct = default);

    /// <summary>Kullanıcı profilini günceller (UserName / Email / Phone dahil).</summary>
    Task<IdentityOperationResult> UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>Kullanıcının şifresini sıfırlar (mevcut şifre doğrulaması olmadan).</summary>
    Task<IdentityOperationResult> ResetPasswordAsync(User user, string newPassword, CancellationToken ct = default);
}

/// <summary>Identity operasyon sonucu (başarı/hata listesi).</summary>
public record IdentityOperationResult(bool Success, string[] Errors)
{
    /// <summary>Başarılı sonuç.</summary>
    public static IdentityOperationResult Ok() => new(true, []);

    /// <summary>Hatalı sonuç.</summary>
    public static IdentityOperationResult Fail(IEnumerable<string> errors) => new(false, errors.ToArray());
}
