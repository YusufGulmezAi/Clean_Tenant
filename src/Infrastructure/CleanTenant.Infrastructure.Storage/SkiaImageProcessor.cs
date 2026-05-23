using CleanTenant.Application.Common.Storage;
using SkiaSharp;

namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// <see cref="IImageProcessor"/>'ın SkiaSharp (MIT) implementasyonu. Görseli
/// merkezden kareye kırpar, hedef boyuta yüksek kaliteyle yeniden ölçekler ve
/// PNG olarak kodlar.
/// </summary>
public sealed class SkiaImageProcessor : IImageProcessor
{
    // Mitchell kübik resampler: küçültmede yumuşak, keskinlik dengeli sonuç.
    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    /// <inheritdoc />
    public Task<ProcessedImage> ToSquarePngAsync(Stream source, int size, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var original = SKBitmap.Decode(source)
            ?? throw new InvalidImageException("Yüklenen dosya tanınan bir görsel formatı değil veya bozuk.");

        // 1) Merkezden en büyük kareyi kırp.
        var side = Math.Min(original.Width, original.Height);
        var srcX = (original.Width - side) / 2;
        var srcY = (original.Height - side) / 2;

        using var cropped = new SKBitmap(side, side);
        if (!original.ExtractSubset(cropped, SKRectI.Create(srcX, srcY, side, side)))
        {
            throw new InvalidImageException("Görsel kırpılamadı.");
        }

        // 2) Hedef kareye ölçekle.
        using var resized = cropped.Resize(new SKImageInfo(size, size), Sampling)
            ?? throw new InvalidImageException("Görsel yeniden boyutlandırılamadı.");

        // 3) PNG'ye kodla.
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var result = new ProcessedImage(data.ToArray(), size, size);
        return Task.FromResult(result);
    }
}
