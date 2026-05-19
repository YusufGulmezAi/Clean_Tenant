using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// Yapı şeması import şablonunu (.xlsx) üretir ve byte dizisi olarak döner.
/// Şablon; dropdown validasyonlu sütunlar, örnek satır ve açıklamalar içerir.
/// </summary>
[RequirePermission("BuildingSchema.Read")]
public sealed record GenerateBuildingSchemaTemplateQuery : IRequest<Result<byte[]>>;
