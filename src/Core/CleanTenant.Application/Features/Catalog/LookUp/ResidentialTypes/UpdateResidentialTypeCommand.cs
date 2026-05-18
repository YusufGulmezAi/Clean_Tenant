using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

[RequirePermission("LookUp.Manage")]
public sealed record UpdateResidentialTypeCommand(Guid Id, string Name, string? Description = null) : IRequest<Result>;
