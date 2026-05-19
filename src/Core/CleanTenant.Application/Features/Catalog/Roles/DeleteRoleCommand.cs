using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;
