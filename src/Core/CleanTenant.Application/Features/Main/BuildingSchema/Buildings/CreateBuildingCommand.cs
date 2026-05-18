using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// Bir Parcel altına yeni Building (yapı/blok) ekler.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record CreateBuildingCommand(
    Guid ParcelId,
    string Name,
    string? MunicipalNo,
    BuildingType Type) : IRequest<Result<Guid>>;
