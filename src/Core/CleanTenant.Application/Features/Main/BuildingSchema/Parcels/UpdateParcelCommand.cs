using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// Mevcut bir Parcel'in adını günceller.
/// </summary>
/// <param name="ParcelId">Güncellenecek parsel.</param>
/// <param name="Name">Yeni parsel adı.</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record UpdateParcelCommand(Guid ParcelId, string Name) : IRequest<Result>;
