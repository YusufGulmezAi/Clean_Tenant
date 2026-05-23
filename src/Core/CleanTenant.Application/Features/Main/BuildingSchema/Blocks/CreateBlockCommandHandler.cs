using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="CreateBlockCommand"/> handler.
/// </summary>
public sealed class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBlockCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBlockCommand command, CancellationToken cancellationToken)
    {
        var buildingExists = await _db.Buildings
            .AnyAsync(b => b.Id == command.BuildingId && !b.IsDeleted, cancellationToken);

        if (!buildingExists)
            return Result<Guid>.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        var nextSortOrder = await _db.Blocks
            .Where(b => b.BuildingId == command.BuildingId && !b.IsDeleted)
            .MaxAsync(b => (int?)b.SortOrder, cancellationToken) ?? 0;

        var block = new Block
        {
            TenantId = _tenantContext.TenantId!.Value,
            BuildingId = command.BuildingId,
            Name = command.Name,
            SortOrder = nextSortOrder + 1,
        };

        _db.Blocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(block.Id);
    }
}
