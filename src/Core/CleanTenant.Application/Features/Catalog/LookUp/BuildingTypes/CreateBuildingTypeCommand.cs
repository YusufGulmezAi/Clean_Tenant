using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

[RequirePermission("LookUp.Manage")]
public sealed record CreateBuildingTypeCommand(string Name, string? Description = null) : IRequest<Result<Guid>>;
