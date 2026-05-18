using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Queries;

/// <summary>
/// Bir Site'nin tüm yapı şemasını (Block → Parcel → Building → Unit) döndürür.
/// </summary>
[RequirePermission("BuildingSchema.Read")]
public sealed record GetBuildingSchemaQuery(Guid CompanyId) : IRequest<Result<BuildingSchemaDto>>;
