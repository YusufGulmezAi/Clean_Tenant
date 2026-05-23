using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Building'deki Block'ların sırasını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderBlocksCommand(
    Guid BuildingId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
