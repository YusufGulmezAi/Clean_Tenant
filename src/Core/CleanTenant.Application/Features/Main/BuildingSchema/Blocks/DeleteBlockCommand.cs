using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// Block'u soft delete eder. Aktif Unit'leri varsa işlem reddedilir.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record DeleteBlockCommand(Guid BlockId) : IRequest<Result>;
