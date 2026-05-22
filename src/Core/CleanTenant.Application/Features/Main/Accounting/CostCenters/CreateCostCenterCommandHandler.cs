using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="CreateCostCenterCommand"/> handler.
/// </summary>
public sealed class CreateCostCenterCommandHandler
    : IRequestHandler<CreateCostCenterCommand, Result<CostCenterListItem>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateCostCenterCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<CostCenterListItem>> Handle(
        CreateCostCenterCommand command,
        CancellationToken cancellationToken)
    {
        // Şirket içinde kod benzersizliği kontrolü
        var exists = await _db.CostCenters
            .AnyAsync(cc => cc.CompanyId == command.CompanyId
                         && cc.Code == command.Code
                         && !cc.IsDeleted, cancellationToken);

        if (exists)
            return Result<CostCenterListItem>.Failure(
                Error.Conflict("ACC-211", $"'{command.Code}' maliyet merkezi kodu zaten mevcut."));

        var costCenter = new CostCenter
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            IsActive = true
        };

        _db.CostCenters.Add(costCenter);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<CostCenterListItem>.Success(new CostCenterListItem(
            costCenter.Id,
            costCenter.Code,
            costCenter.Name,
            costCenter.Description,
            costCenter.IsActive));
    }
}
