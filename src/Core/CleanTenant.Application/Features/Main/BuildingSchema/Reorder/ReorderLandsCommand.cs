using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// Bir Site'deki Land'lerin sırasını günceller.
/// </summary>
/// <param name="CompanyId">Sıralanacak Land'lerin ait olduğu site.</param>
/// <param name="OrderedIds">Yeni sıraya göre Land ID listesi (ilk = 1. sıra).</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ReorderLandsCommand(
    Guid CompanyId,
    IReadOnlyList<Guid> OrderedIds) : IRequest<Result>;
