using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUnit = CleanTenant.Domain.Tenant.BuildingSchema.Unit;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="CreateUnitCommand"/> handler.
/// </summary>
public sealed class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateUnitCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var buildingExists = await _db.Buildings
            .AnyAsync(b => b.Id == command.BuildingId, cancellationToken);
        if (!buildingExists)
            return Result<Guid>.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        // Blok verildiyse: var olmalı ve aynı binaya ait olmalı (BB hem Building hem Block taşır).
        if (command.BlockId.HasValue)
        {
            var blockMatchesBuilding = await _db.Blocks
                .AnyAsync(b => b.Id == command.BlockId.Value
                            && b.BuildingId == command.BuildingId, cancellationToken);
            if (!blockMatchesBuilding)
                return Result<Guid>.Failure(Error.Validation(
                    "BLOCK-BUILDING-MISMATCH", "Seçilen blok bu binaya ait değil."));
        }

        // Numara kapsam içinde benzersiz olmalı (DB unique index'leri: ix_units_block_number /
        // ix_units_building_number ile birebir uyumlu). Önden kontrol → SaveChanges'te unique
        // violation exception atıp (uzun ömürlü) DbContext'i zehirlemesini önler, kullanıcıya
        // temiz hata döner.
        var numberTaken = command.BlockId.HasValue
            ? await _db.Units.AnyAsync(u => u.BlockId == command.BlockId.Value
                                         && u.Number == command.Number && !u.IsDeleted, cancellationToken)
            : await _db.Units.AnyAsync(u => u.BuildingId == command.BuildingId && u.BlockId == null
                                         && u.Number == command.Number && !u.IsDeleted, cancellationToken);
        if (numberTaken)
            return Result<Guid>.Failure(Error.Conflict(
                "UNIT-NUMBER-DUPLICATE", $"\"{command.Number}\" numarası bu kapsamda zaten kullanılıyor."));

        var nextSortOrder = await _db.Units
            .Where(u => u.BuildingId == command.BuildingId)
            .MaxAsync(u => (int?)u.SortOrder, cancellationToken) ?? 0;

        var unit = new DomainUnit
        {
            TenantId = _tenantContext.TenantId!.Value,
            BuildingId = command.BuildingId,
            BlockId = command.BlockId,
            Number = command.Number,
            NationalAddressCode = command.NationalAddressCode,
            Type = command.Type,
            SquareMeters = command.SquareMeters,
            LandShare = command.LandShare,
            AllocatedArea = command.AllocatedArea,
            Floor = command.Floor,
            Orientation = command.Orientation,
            Layout = command.Layout,
            SortOrder = nextSortOrder + 1,
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(unit.Id);
    }
}
