using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Auth.PasswordReset;

/// <summary><see cref="RequestPasswordResetCommand"/> handler.</summary>
public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly IVerificationCodeService _codes;
    private readonly IEmailSender _email;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RequestPasswordResetCommandHandler(
        ICatalogDbContext db,
        IVerificationCodeService codes,
        IEmailSender email)
    {
        _db = db;
        _codes = codes;
        _email = email;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Email == command.Email.ToLowerInvariant() && !u.IsDeleted,
                cancellationToken);

        // Kullanıcı bulunamasa bile başarı döndür (enumeration koruması).
        if (user is null) return Result.Success();

        var key = $"pwd-reset:{user.Id}";
        var code = await _codes.GenerateAsync(key, TimeSpan.FromMinutes(15), cancellationToken);

        await _email.SendAsync(
            user.Email!,
            "Şifre Sıfırlama",
            $"Şifre sıfırlama kodunuz: {code}\nBu kod 15 dakika geçerlidir.",
            cancellationToken);

        return Result.Success();
    }
}
