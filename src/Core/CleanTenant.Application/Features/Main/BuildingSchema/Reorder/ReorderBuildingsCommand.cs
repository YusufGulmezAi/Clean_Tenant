using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Parcel'daki Building'lerin sırasını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderBuildingsCommand(
    Guid ParcelId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
