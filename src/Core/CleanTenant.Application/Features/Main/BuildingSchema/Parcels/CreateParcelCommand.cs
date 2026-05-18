using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// Bir Block altına yeni Parcel ekler.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record CreateParcelCommand(
    Guid BlockId,
    string Name) : IRequest<Result<Guid>>;
