using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Auth.PasswordReset;

/// <summary><see cref="ResetPasswordWithCodeCommand"/> handler.</summary>
public sealed class ResetPasswordWithCodeCommandHandler : IRequestHandler<ResetPasswordWithCodeCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly IVerificationCodeService _codes;
    private readonly UserManager<User> _userManager;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ResetPasswordWithCodeCommandHandler(
        ICatalogDbContext db,
        IVerificationCodeService codes,
        UserManager<User> userManager)
    {
        _db = db;
        _codes = codes;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ResetPasswordWithCodeCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Email == command.Email.ToLowerInvariant() && !u.IsDeleted,
                cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("AUTH-030", "Kullanıcı bulunamadı."));

        var key = $"pwd-reset:{user.Id}";
        var valid = await _codes.VerifyAsync(key, command.Code, cancellationToken);
        if (!valid)
            return Result.Failure(Error.Validation("AUTH-031", "Doğrulama kodu geçersiz veya süresi dolmuş."));

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, command.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("AUTH-032", $"Şifre değiştirilemedi: {errors}"));
        }

        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}
