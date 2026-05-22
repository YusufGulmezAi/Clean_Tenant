using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Land (Ada) altındaki Parcel'ların sırasını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderParcelsCommand(
    Guid LandId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
