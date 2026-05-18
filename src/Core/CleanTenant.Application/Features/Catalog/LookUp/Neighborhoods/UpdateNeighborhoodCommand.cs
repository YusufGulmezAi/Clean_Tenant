using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

[RequirePermission("LookUp.Manage")]
public sealed record UpdateNeighborhoodCommand(Guid Id, string Name, Guid DistrictId) : IRequest<Result>;
