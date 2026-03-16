using SkiaSharp;

namespace MonsterBot.Services;

public static class ImageCompressor
{
    private const int MaxDimension = 800;
    private const int JpegQuality = 65;
    private const int MaxBytes = 500 * 1024; // 500 Ko

    public static (byte[] Bytes, string MediaType) Compress(byte[] input)
    {
        using var original = SKBitmap.Decode(input);
        if (original is null)
            return (input, "image/jpeg");

        var bitmap = Resize(original);

        using var image = SKImage.FromBitmap(bitmap);
        var bytes = Encode(image, JpegQuality);

        // Deuxième passe si encore trop lourd
        if (bytes.Length > MaxBytes)
            bytes = Encode(image, 45);

        if (bitmap != original) bitmap.Dispose();

        return (bytes, "image/jpeg");
    }

    private static byte[] Encode(SKImage image, int quality)
    {
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    private static SKBitmap Resize(SKBitmap src)
    {
        if (src.Width <= MaxDimension && src.Height <= MaxDimension)
            return src;

        var ratio = Math.Min((float)MaxDimension / src.Width, (float)MaxDimension / src.Height);
        var newWidth = (int)(src.Width * ratio);
        var newHeight = (int)(src.Height * ratio);

        return src.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
    }
}
