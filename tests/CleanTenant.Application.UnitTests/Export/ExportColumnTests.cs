using CleanTenant.Application.Common.Export;

namespace CleanTenant.Application.UnitTests.Export;

/// <summary>
/// <see cref="ExportColumn{T}"/> DTO davranış testleri — record-init pattern'i,
/// selector çağrısı, null değer toleransı.
/// </summary>
public sealed class ExportColumnTests
{
    private sealed record Row(string Name, int Count, string? Note);

    [Fact]
    public void Init_required_alanlar_ile_olusur()
    {
        var col = new ExportColumn<Row>
        {
            Header = "Ad",
            Selector = r => r.Name,
        };

        col.Header.Should().Be("Ad");
        col.Format.Should().BeNull();
        col.RelativeWidth.Should().Be(0);
    }

    [Fact]
    public void Selector_satir_propertysini_dondurmeli()
    {
        var col = new ExportColumn<Row>
        {
            Header = "Adet",
            Selector = r => r.Count,
        };

        var row = new Row("alfa", 42, null);
        col.Selector(row).Should().Be(42);
    }

    [Fact]
    public void Selector_nullable_property_null_donebilmeli()
    {
        var col = new ExportColumn<Row>
        {
            Header = "Not",
            Selector = r => r.Note,
        };

        var row = new Row("alfa", 1, null);
        col.Selector(row).Should().BeNull();
    }

    [Fact]
    public void Format_ve_RelativeWidth_init_edilebilir()
    {
        var col = new ExportColumn<Row>
        {
            Header = "Adet",
            Selector = r => r.Count,
            Format = "N2",
            RelativeWidth = 2.5f,
        };

        col.Format.Should().Be("N2");
        col.RelativeWidth.Should().Be(2.5f);
    }
}
