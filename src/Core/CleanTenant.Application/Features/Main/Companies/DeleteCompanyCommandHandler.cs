using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <see cref="DeleteCompanyCommand"/> handler. Site'yi soft-delete eder (IsDeleted = true).
/// </summary>
public sealed class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, Result<Unit>>
{
    private readonly IMainDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteCompanyCommandHandler(
        IMainDbContext db,
        ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> Handle(DeleteCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == command.CompanyId, cancellationToken);

        if (company is null)
            return Result<Unit>.Failure(
                Error.NotFound("COMPANY-NOT-FOUND", "Site bulunamadı."));

        company.IsDeleted = true;
        company.DeletedAt = DateTimeOffset.UtcNow;

        _db.Companies.Update(company);
        await _db.SaveChangesAsync(cancellationToken);

        // Cache'i invalidate et
        await _cacheInvalidator.InvalidateCompanyAsync(company.Id, company.TenantId, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
