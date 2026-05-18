using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// Mevcut bir Building'in bilgilerini günceller.
/// </summary>
/// <param name="BuildingId">Güncellenecek yapı.</param>
/// <param name="Name">Yapı adı.</param>
/// <param name="MunicipalNo">Belediye numarası (opsiyonel).</param>
/// <param name="Type">Yapı tipi.</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record UpdateBuildingCommand(
    Guid BuildingId,
    string Name,
    string? MunicipalNo,
    BuildingType Type) : IRequest<Result>;
