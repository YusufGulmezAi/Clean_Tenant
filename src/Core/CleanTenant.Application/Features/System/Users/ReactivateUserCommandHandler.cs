using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="ReactivateUserCommand"/> handler. Devre dışı kullanıcıyı aktif eder.
/// </summary>
public sealed class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReactivateUserCommandHandler(ICatalogDbContext db, ICurrentSessionAccessor session)
    {
        _db = db;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.UrlCode == command.UrlCode && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        if (user.IsActive)
            return Result.Failure(Error.Conflict("USER-009", "Kullanıcı zaten aktif."));

        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = _session.Current?.UserId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
