using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// Excel dosyasından yapı şeması hiyerarşisini (Land → Parcel → Building → Unit)
/// toplu import eder. Hata varsa error workbook döner; hata yoksa upsert yapar.
/// </summary>
/// <param name="CompanyId">Import hedef site.</param>
/// <param name="ExcelStream">Şablon formatında dolu .xlsx stream.</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record ImportBuildingSchemaCommand(
    Guid CompanyId,
    Stream ExcelStream) : IRequest<Result<ImportBuildingSchemaResult>>;

/// <summary>
/// <see cref="ImportBuildingSchemaCommand"/> sonuç DTO'su.
/// </summary>
/// <param name="HasErrors">Validasyon hatası var mı.</param>
/// <param name="ErrorWorkbook">Hata varsa kırmızı sütunlu workbook bytes, yoksa <c>null</c>.</param>
/// <param name="ImportedUnitCount">Başarıyla upsert edilen BB sayısı.</param>
public sealed record ImportBuildingSchemaResult(
    bool HasErrors,
    byte[]? ErrorWorkbook,
    int ImportedUnitCount);
