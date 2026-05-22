using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="CreateLandCommand"/> handler.
/// </summary>
public sealed class CreateLandCommandHandler : IRequestHandler<CreateLandCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateLandCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateLandCommand command, CancellationToken cancellationToken)
    {
        var companyExists = await _db.Companies
            .AnyAsync(c => c.Id == command.CompanyId, cancellationToken);
        if (!companyExists)
            return Result<Guid>.Failure(Error.NotFound("COMPANY-NOT-FOUND", "Site bulunamadı."));

        var nextSortOrder = await _db.Lands
            .Where(l => l.CompanyId == command.CompanyId)
            .MaxAsync(l => (int?)l.SortOrder, cancellationToken) ?? 0;

        var land = new Land
        {
            TenantId = _tenantContext.TenantId!.Value,
            CompanyId = command.CompanyId,
            Name = command.Name,
            SortOrder = nextSortOrder + 1,
        };

        _db.Lands.Add(land);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(land.Id);
    }
}
