using CleanTenant.Application.Common.Export;
using CleanTenant.Infrastructure.Export.Excel;
using ClosedXML.Excel;

namespace CleanTenant.Infrastructure.IntegrationTests.Export;

/// <summary>
/// <see cref="ClosedXmlExportService"/> davranış testleri — gerçek ClosedXML ile
/// .xlsx byte üretimi + roundtrip parse.
/// </summary>
public sealed class ClosedXmlExportServiceTests
{
    private sealed record Row(string Name, int Count, DateTime Date);

    [Fact]
    public void Generate_xlsx_byte_donmeli_ve_header_data_iceriklerini_tasimali()
    {
        var rows = new[]
        {
            new Row("Alfa", 7, new DateTime(2026, 1, 15)),
            new Row("Beta", 12, new DateTime(2026, 2, 20)),
        };
        var cols = new ExportColumn<Row>[]
        {
            new() { Header = "Ad", Selector = r => r.Name },
            new() { Header = "Adet", Selector = r => r.Count },
            new() { Header = "Tarih", Selector = r => r.Date, Format = "yyyy-MM-dd" },
        };

        var svc = new ClosedXmlExportService();
        var bytes = svc.Generate(rows, cols, "Test Sheet");

        bytes.Should().NotBeEmpty();

        // Roundtrip parse
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheets.First();

        sheet.Name.Should().Be("Test Sheet");
        sheet.Cell(1, 1).GetString().Should().Be("Ad");
        sheet.Cell(1, 2).GetString().Should().Be("Adet");
        sheet.Cell(1, 3).GetString().Should().Be("Tarih");
        sheet.Cell(2, 1).GetString().Should().Be("Alfa");
        sheet.Cell(2, 2).GetValue<int>().Should().Be(7);
        sheet.Cell(3, 1).GetString().Should().Be("Beta");
    }

    [Fact]
    public void Generate_bos_satir_yine_header_uretmeli()
    {
        var cols = new ExportColumn<Row>[]
        {
            new() { Header = "Ad", Selector = r => r.Name },
        };
        var svc = new ClosedXmlExportService();

        var bytes = svc.Generate(Array.Empty<Row>(), cols, "Empty");

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheets.First();
        sheet.Cell(1, 1).GetString().Should().Be("Ad");
    }

    [Fact]
    public void Generate_kolon_yoksa_exception()
    {
        var svc = new ClosedXmlExportService();
        var act = () => svc.Generate(Array.Empty<Row>(), Array.Empty<ExportColumn<Row>>(), "x");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_uzun_sheet_adi_31_karaktere_kisalmali()
    {
        var cols = new ExportColumn<Row>[]
        {
            new() { Header = "Ad", Selector = r => r.Name },
        };
        var svc = new ClosedXmlExportService();
        var bytes = svc.Generate(Array.Empty<Row>(), cols, new string('A', 50));

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        workbook.Worksheets.First().Name.Length.Should().BeLessOrEqualTo(31);
    }
}
