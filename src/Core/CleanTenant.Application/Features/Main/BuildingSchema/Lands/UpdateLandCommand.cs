using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// Mevcut bir Land'in adını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record UpdateLandCommand(
    Guid LandId,
    string Name) : IRequest<Result>;
