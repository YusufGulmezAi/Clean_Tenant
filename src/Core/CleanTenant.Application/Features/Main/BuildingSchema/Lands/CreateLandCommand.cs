using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// Bir Site (Company) altına yeni Ada (Land) ekler.
/// </summary>
/// <param name="CompanyId">Land'in ait olduğu site.</param>
/// <param name="Name">Ada adı veya numarası (örn. "123", "A", "0").</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record CreateLandCommand(
    Guid CompanyId,
    string Name) : IRequest<Result<Guid>>;
