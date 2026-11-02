using ImageMagick;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Media;

internal static class AvifImage
{
    public static SKBitmap Decode(Stream content)
    {
        var info = new MagickImageInfo(content);
        if (info.Format != MagickFormat.Avif || info.Width == 0 || info.Height == 0 ||
            (ulong)info.Width * info.Height > 40_000_000)
            throw new InvalidDataException("The AVIF dimensions or format are invalid.");
        content.Position = 0;
        using var image = new MagickImage(content, MagickFormat.Avif);
        return SKBitmap.Decode(image.ToByteArray(MagickFormat.Png))
            ?? throw new InvalidDataException("The AVIF image cannot be decoded.");
    }

    public static bool HasSignature(Stream content)
    {
        Span<byte> header = stackalloc byte[32];
        var count = content.Read(header);
        content.Position = 0;
        return count >= 16 && header[4..8].SequenceEqual("ftyp"u8) &&
            (header[8..count].IndexOf("avif"u8) >= 0 || header[8..count].IndexOf("avis"u8) >= 0);
    }
}
