namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// Yapı şeması Excel şablonu üretimi ve import parsing sözleşmesi.
/// Infrastructure katmanı tarafından ClosedXML ile implemente edilir.
/// </summary>
public interface IBuildingSchemaExcelService
{
    /// <summary>Boş şablon .xlsx byte dizisini üretir; dropdown validasyonlu.</summary>
    byte[] GenerateTemplate();

    /// <summary>
    /// Import dosyasını okur, satır bazında validasyon uygular.
    /// Hata varsa <see cref="BuildingSchemaParseResult.HasErrors"/> = <c>true</c>
    /// ve <see cref="BuildingSchemaParseResult.ErrorWorkbook"/> dolu döner.
    /// </summary>
    BuildingSchemaParseResult ParseAndValidate(Stream excelStream);
}

/// <summary>
/// <see cref="IBuildingSchemaExcelService.ParseAndValidate"/> çıkışı.
/// </summary>
public sealed class BuildingSchemaParseResult
{
    /// <summary>En az bir satırda validasyon hatası var mı.</summary>
    public bool HasErrors { get; init; }

    /// <summary>Hata varsa kırmızı hata sütunlu workbook bytes, yoksa <c>null</c>.</summary>
    public byte[]? ErrorWorkbook { get; init; }

    /// <summary>Başarıyla parse edilmiş satırlar; <see cref="HasErrors"/> = <c>true</c> ise boş.</summary>
    public IReadOnlyList<BuildingSchemaImportRow> Rows { get; init; } = [];
}
