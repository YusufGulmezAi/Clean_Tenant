using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// Mevcut bir Block'un adını günceller.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record UpdateBlockCommand(
    Guid BlockId,
    string Name) : IRequest<Result>;
