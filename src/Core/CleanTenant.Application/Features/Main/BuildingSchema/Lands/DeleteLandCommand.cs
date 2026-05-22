using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// Land'i soft delete eder. Aktif Parcel'leri varsa işlem reddedilir.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record DeleteLandCommand(Guid LandId) : IRequest<Result>;
