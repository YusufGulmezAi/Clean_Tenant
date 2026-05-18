using CleanTenant.Application.Common.Export;
using CleanTenant.Infrastructure.Export.Pdf;
using QuestPDF.Infrastructure;

namespace CleanTenant.Infrastructure.IntegrationTests.Export;

/// <summary>
/// <see cref="QuestPdfExportService"/> davranış testleri — gerçek QuestPDF ile
/// PDF byte üretimi + PDF magic bytes doğrulaması.
/// </summary>
public sealed class QuestPdfExportServiceTests
{
    private sealed record Row(string Name, decimal Tutar);

    public QuestPdfExportServiceTests()
    {
        // Test fixture'da lisans set'lemesi tekrarlanırsa zararsız.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Generate_pdf_magic_bytes_iliskili_donmeli()
    {
        var rows = new[] { new Row("Alfa", 1234.56m), new Row("Beta", 999m) };
        var cols = new ExportColumn<Row>[]
        {
            new() { Header = "Ad", Selector = r => r.Name },
            new() { Header = "Tutar", Selector = r => r.Tutar, Format = "N2" },
        };

        var svc = new QuestPdfExportService();
        var bytes = svc.Generate(rows, cols, "Test Rapor");

        bytes.Should().NotBeEmpty();
        // PDF magic bytes: "%PDF-"
        bytes.Take(5).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }

    [Fact]
    public void Generate_bos_satir_yine_pdf_uretmeli()
    {
        var cols = new ExportColumn<Row>[]
        {
            new() { Header = "Ad", Selector = r => r.Name },
        };
        var svc = new QuestPdfExportService();

        var bytes = svc.Generate(Array.Empty<Row>(), cols, "Empty");

        bytes.Should().NotBeEmpty();
        bytes.Take(5).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }

    [Fact]
    public void Generate_kolon_yoksa_exception()
    {
        var svc = new QuestPdfExportService();
        var act = () => svc.Generate(Array.Empty<Row>(), Array.Empty<ExportColumn<Row>>(), "x");

        act.Should().Throw<ArgumentException>();
    }
}
