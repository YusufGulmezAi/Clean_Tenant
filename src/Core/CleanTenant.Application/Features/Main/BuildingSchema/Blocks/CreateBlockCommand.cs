using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// Bir Building (yapı) altına yeni Block (kule/blok) ekler.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record CreateBlockCommand(
    Guid BuildingId,
    string Name) : IRequest<Result<Guid>>;
