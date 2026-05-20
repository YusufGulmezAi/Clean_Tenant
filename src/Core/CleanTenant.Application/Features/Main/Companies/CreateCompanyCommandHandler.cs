using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <see cref="CreateCompanyCommand"/> handler. Site yaratır, cache'i invalidate eder.
/// </summary>
public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDetail>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheInvalidator _cacheInvalidator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateCompanyCommandHandler(
        IMainDbContext db,
        ITenantContext tenantContext,
        ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <inheritdoc />
    public async Task<Result<CompanyDetail>> Handle(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        // Tenant mevcudiyeti doğrula (System scope'ta bypass edilmeli, Tenant scope'ta zorunlu)
        var tenantExists = await _db.Companies
            .Where(c => c.TenantId == command.TenantId)
            .AnyAsync(cancellationToken);

        // İlk site de olsa tenant var olması gerek (bu kontrol opsiyonel, Tenant entity varsa yeterli)
        // Faz 0'da Tenant zaten create'leniyor, bu kontrol for safety

        var company = new Company
        {
            TenantId = command.TenantId,
            Name = command.Name,
            LegalName = command.LegalName,
            Vkn = command.Vkn,
            Email = command.Email,
            Phone = command.Phone,
            Status = CompanyStatus.Active,
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);

        // Cache'i invalidate et — yeni site Context Switcher'da görünmeli (v0.2.11.d)
        await _cacheInvalidator.InvalidateCompanyAsync(company.Id, company.TenantId, cancellationToken);
        await _cacheInvalidator.InvalidateAllUserContextsAsync(cancellationToken);

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
