using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// Building'i soft delete eder. Aktif Unit'leri varsa işlem reddedilir.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record DeleteBuildingCommand(Guid BuildingId) : IRequest<Result>;
