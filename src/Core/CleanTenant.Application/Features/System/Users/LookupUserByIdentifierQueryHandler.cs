using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="LookupUserByIdentifierQuery"/> handler.
/// İlgili identifier tipine göre kullanıcıyı arar; bulunamazsa null Result döner.
/// </summary>
public sealed class LookupUserByIdentifierQueryHandler
    : IRequestHandler<LookupUserByIdentifierQuery, Result<UserLookupResult?>>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LookupUserByIdentifierQueryHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<UserLookupResult?>> Handle(
        LookupUserByIdentifierQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Value))
        {
            return Result<UserLookupResult?>.Success(null);
        }

        var value = query.Value.Trim();

        var user = query.Type switch
        {
            UserLookupType.Tckn
                => await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Tckn == value && !u.IsDeleted, cancellationToken),

            UserLookupType.Vkn
                => await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Vkn == value && !u.IsDeleted, cancellationToken),

            UserLookupType.Phone
                => await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.PhoneNumber == value && !u.IsDeleted, cancellationToken),

            UserLookupType.Email
                => await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == value && !u.IsDeleted, cancellationToken),

            _ => null,
        };

        if (user is null)
        {
            return Result<UserLookupResult?>.Success(null);
        }

        return Result<UserLookupResult?>.Success(new UserLookupResult(
            user.Id,
            user.UrlCode,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.IsActive));
    }
}
