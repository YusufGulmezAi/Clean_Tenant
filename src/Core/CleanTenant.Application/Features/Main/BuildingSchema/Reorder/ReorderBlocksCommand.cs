using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Site'deki Block'ların sırasını günceller.
/// </summary>
/// <param name="CompanyId">Sıralanacak Block'ların ait olduğu site.</param>
/// <param name="OrderedIds">Yeni sıraya göre Block ID listesi (ilk = 1. sıra).</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderBlocksCommand(
    Guid CompanyId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
