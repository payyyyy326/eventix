using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Eventix.Infrastructure.QrCode;

public class QrCodeImageDecoder : IQrCodeImageDecoder
{
    private const long MaxPixels = 20_000_000;

    public Task<string?> DecodeAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Decode(imageStream), cancellationToken);
    }

    private static string? Decode(Stream imageStream)
    {
        using var source = SKBitmap.Decode(imageStream);
        if (source == null)
            return null;

        var pixelCount = (long)source.Width * source.Height;
        if (pixelCount <= 0 || pixelCount > MaxPixels)
            return null;

        var imageInfo = new SKImageInfo(
            source.Width,
            source.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imageInfo);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(source, 0, 0);
            canvas.Flush();
        }

        var pixels = new byte[checked(bitmap.RowBytes * bitmap.Height)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);

        var luminanceSource = new RGBLuminanceSource(
            pixels,
            bitmap.Width,
            bitmap.Height,
            RGBLuminanceSource.BitmapFormat.BGRA32);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.QR_CODE]
            }
        };

        return reader.Decode(luminanceSource)?.Text;
    }
}