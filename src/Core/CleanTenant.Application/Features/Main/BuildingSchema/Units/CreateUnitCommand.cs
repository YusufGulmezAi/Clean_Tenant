using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// Bir Building altına yeni Unit (bağımsız bölüm) ekler.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record CreateUnitCommand(
    Guid BuildingId,
    string Number,
    string? NationalAddressCode,
    UnitType Type,
    decimal SquareMeters,
    int LandShare,
    decimal? AllocatedArea,
    int Floor,
    Orientation Orientation,
    ApartmentLayout Layout,
    // Opsiyonel: BB'nin bağlanacağı Blok. Null → doğrudan Building'e bağlanır.
    // (Sona eklendi ki mevcut konum-bazlı çağrılar kırılmasın.)
    Guid? BlockId = null) : IRequest<Result<Guid>>;
