using CleanTenant.Application.Common.Storage;
using CleanTenant.Infrastructure.Storage;
using SkiaSharp;

namespace CleanTenant.Infrastructure.IntegrationTests.Storage;

/// <summary>
/// v0.2.13 — <see cref="SkiaImageProcessor"/> birim testleri (DB gerekmez):
/// kare olmayan görseli 100x100 PNG'ye getirmeli; bozuk girdide hata fırlatmalı.
/// </summary>
public sealed class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    [Theory]
    [InlineData(300, 200)] // yatay
    [InlineData(200, 300)] // dikey
    [InlineData(100, 100)] // zaten kare
    [InlineData(40, 90)]   // küçük, dikey
    public async Task Kare_olmayan_gorsel_100x100_PNG_e_donmeli(int width, int height)
    {
        using var source = CreateTestPng(width, height);

        var result = await _processor.ToSquarePngAsync(source, 100);

        result.Width.Should().Be(100);
        result.Height.Should().Be(100);

        // Çıktı gerçekten 100x100 PNG olarak çözümlenebilmeli.
        using var decoded = SKBitmap.Decode(result.Content);
        decoded.Should().NotBeNull();
        decoded!.Width.Should().Be(100);
        decoded.Height.Should().Be(100);
    }

    [Fact]
    public async Task Bozuk_girdi_InvalidImageException_firlatmali()
    {
        using var garbage = new MemoryStream([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);

        var act = async () => await _processor.ToSquarePngAsync(garbage, 100);

        await act.Should().ThrowAsync<InvalidImageException>();
    }

    private static MemoryStream CreateTestPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.CornflowerBlue);
            using var paint = new SKPaint { Color = SKColors.OrangeRed };
            canvas.DrawRect(0, 0, width / 2f, height / 2f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }
}
