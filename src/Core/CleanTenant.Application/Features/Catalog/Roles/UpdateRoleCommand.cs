using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest;
