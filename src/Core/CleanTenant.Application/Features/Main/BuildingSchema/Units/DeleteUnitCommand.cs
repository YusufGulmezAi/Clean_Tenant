using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// Unit'i soft delete eder.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record DeleteUnitCommand(Guid UnitId) : IRequest<Result>;
