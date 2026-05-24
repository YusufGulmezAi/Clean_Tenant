using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="UnlockUserCommand"/> handler. Kullanıcıyı UrlCode ile bulur ve
/// <see cref="IUserRepository.UnlockAsync"/> ile kilidini açar.
/// </summary>
public sealed class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UnlockUserCommandHandler(ICatalogDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UnlockUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.UrlCode == command.UrlCode && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        var result = await _userRepo.UnlockAsync(user, cancellationToken);
        if (!result.Success)
            return Result.Failure(Error.Validation("USER-013", string.Join(" ", result.Errors)));

        return Result.Success();
    }
}
