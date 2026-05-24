using CleanTenant.Application.Common.Identity;
using CleanTenant.Domain.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Infrastructure.Identity.Users;

/// <summary>
/// <see cref="IUserRepository"/> ASP.NET Core Identity <see cref="UserManager{TUser}"/>
/// ile gerçekleştirimi. Şifre hash'leme, güncelleme ve sıfırlama işlemlerini sarar.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly UserManager<User> _userManager;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UserRepository(UserManager<User> userManager) => _userManager = userManager;

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is null) return false;
        return excludeUserId is null || existing.Id != excludeUserId.Value;
    }

    /// <inheritdoc />
    public async Task<IdentityOperationResult> CreateAsync(User user, string password, CancellationToken ct = default)
    {
        var result = await _userManager.CreateAsync(user, password);
        return result.Succeeded
            ? IdentityOperationResult.Ok()
            : IdentityOperationResult.Fail(result.Errors.Select(e => e.Description));
    }

    /// <inheritdoc />
    public async Task<IdentityOperationResult> UpdateAsync(User user, CancellationToken ct = default)
    {
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? IdentityOperationResult.Ok()
            : IdentityOperationResult.Fail(result.Errors.Select(e => e.Description));
    }

    /// <inheritdoc />
    public async Task<IdentityOperationResult> ResetPasswordAsync(User user, string newPassword, CancellationToken ct = default)
    {
        // Mevcut şifre hash'ini sil + yenisini set et (admin reset — token yok)
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return IdentityOperationResult.Fail(removeResult.Errors.Select(e => e.Description));

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        return addResult.Succeeded
            ? IdentityOperationResult.Ok()
            : IdentityOperationResult.Fail(addResult.Errors.Select(e => e.Description));
    }

    /// <inheritdoc />
    public async Task<IdentityOperationResult> UnlockAsync(User user, CancellationToken ct = default)
    {
        // Kilidi kaldır (LockoutEnd = null) ve hatalı deneme sayacını sıfırla.
        var clearResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!clearResult.Succeeded)
            return IdentityOperationResult.Fail(clearResult.Errors.Select(e => e.Description));

        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        return resetResult.Succeeded
            ? IdentityOperationResult.Ok()
            : IdentityOperationResult.Fail(resetResult.Errors.Select(e => e.Description));
    }
}
