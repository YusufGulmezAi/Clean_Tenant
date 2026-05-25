using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Infrastructure.Export.BuildingSchema;
using ClosedXML.Excel;
using Xunit;

namespace CleanTenant.Infrastructure.IntegrationTests.BuildingSchema;

/// <summary>
/// <see cref="BuildingSchemaExcelService"/> import validasyon davranışı:
/// Yön/Oda-Salon boş geçilebilir ama listede olmayan bir değer hata üretmeli.
/// (Saf servis — DB/container gerektirmez.)
/// </summary>
public sealed class BuildingSchemaExcelServiceTests
{
    private const int DataStart = 28; // şablondaki ilk veri satırı (başlığın hemen altı)

    private static MemoryStream BuildWorkbook(string? orientation, string? layout, decimal? allocatedArea = null)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Şablon");
        int r = DataStart;
        ws.Cell(r, 1).Value = "1";      // Ada
        ws.Cell(r, 2).Value = "1";      // Parsel
        ws.Cell(r, 3).Value = "A";      // Yapı
        ws.Cell(r, 4).Value = "Konut";  // Yapı Tipi
        // 5 Belediye No — boş (opsiyonel)
        // 6 Blok — boş (opsiyonel)
        ws.Cell(r, 7).Value = "1";      // BB No
        ws.Cell(r, 8).Value = "Daire";  // BB Tipi
        ws.Cell(r, 9).Value = 85;       // m²
        ws.Cell(r, 10).Value = 10;      // Arsa Payı
        if (allocatedArea is not null) ws.Cell(r, 11).Value = allocatedArea.Value; // Tahsis Alanı
        if (orientation is not null) ws.Cell(r, 12).Value = orientation; // Yön
        ws.Cell(r, 13).Value = 1;       // Kat
        if (layout is not null) ws.Cell(r, 14).Value = layout;           // Oda/Salon
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void EmptyOrientationAndLayout_NoError()
    {
        var svc = new BuildingSchemaExcelService();
        using var ms = BuildWorkbook(orientation: null, layout: null);
        var result = svc.ParseAndValidate(ms);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void InvalidOrientation_ProducesError()
    {
        var svc = new BuildingSchemaExcelService();
        using var ms = BuildWorkbook(orientation: "GeçersizYön", layout: null);
        var result = svc.ParseAndValidate(ms);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void InvalidLayout_ProducesError()
    {
        var svc = new BuildingSchemaExcelService();
        using var ms = BuildWorkbook(orientation: null, layout: "GeçersizOda");
        var result = svc.ParseAndValidate(ms);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void AllocatedAreaZero_NoError()
    {
        var svc = new BuildingSchemaExcelService();
        using var ms = BuildWorkbook(orientation: null, layout: null, allocatedArea: 0m);
        var result = svc.ParseAndValidate(ms);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void EmptyLayout_DefaultsToUnknown()
    {
        var svc = new BuildingSchemaExcelService();
        using var ms = BuildWorkbook(orientation: null, layout: null);
        var result = svc.ParseAndValidate(ms);
        Assert.False(result.HasErrors);
        Assert.Equal(ApartmentLayout.Unknown, Assert.Single(result.Rows).Layout);
    }
}
