using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// Parcel'i soft delete eder. Aktif Building'leri varsa işlem reddedilir.
/// </summary>
[RequirePermission("BuildingSchema.Manage")]
public sealed record DeleteParcelCommand(Guid ParcelId) : IRequest<Result>;
