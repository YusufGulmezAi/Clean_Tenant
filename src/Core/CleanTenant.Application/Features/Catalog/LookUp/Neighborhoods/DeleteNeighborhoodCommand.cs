using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

[RequirePermission("LookUp.Manage")]
public sealed record DeleteNeighborhoodCommand(Guid Id) : IRequest<Result>;
