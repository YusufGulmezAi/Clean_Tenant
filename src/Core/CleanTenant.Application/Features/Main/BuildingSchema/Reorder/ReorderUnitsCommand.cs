using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Building'deki Unit'lerin sırasını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderUnitsCommand(
    Guid BuildingId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
