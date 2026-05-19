using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record CreateRoleCommand(
    string Name,
    int Scope,
    string? Description) : IRequest<Guid>;
