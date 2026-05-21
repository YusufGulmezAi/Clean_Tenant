using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <see cref="UpdateCompanyCommand"/> handler. Site bilgilerini günceller.
/// </summary>
public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result<CompanyDetail>>
{
    private readonly IMainDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateCompanyCommandHandler(
        IMainDbContext db,
        ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <inheritdoc />
    public async Task<Result<CompanyDetail>> Handle(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == command.CompanyId, cancellationToken);

        if (company is null)
            return Result<CompanyDetail>.Failure(
                Error.NotFound("COMPANY-NOT-FOUND", "Site bulunamadı."));

        company.Name = command.Name;
        company.LegalName = command.LegalName;
        company.Vkn = command.Vkn;
        company.Email = command.Email;
        company.Phone = command.Phone;
        company.Status = command.Status;

        _db.Companies.Update(company);
        await _db.SaveChangesAsync(cancellationToken);

        // Cache'i invalidate et
        await _cacheInvalidator.InvalidateCompanyAsync(company.Id, company.TenantId, cancellationToken);

        return Result<CompanyDetail>.Success(new CompanyDetail(
            company.Id,
            company.TenantId,
            company.UrlCode,
            company.Name,
            company.LegalName,
            company.Vkn,
            company.Email,
            company.Phone,
            company.Status));
    }
}
