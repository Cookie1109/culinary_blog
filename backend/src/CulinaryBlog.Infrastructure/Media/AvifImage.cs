// Culinary Blog. Authorship is recorded in the repository history.

using ImageMagick;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Media;

/// <summary>Represents the avif image.</summary>
internal static class AvifImage
{
    /// <summary>Performs the decode operation.</summary>
    /// <param name="content">The content supplied to this operation.</param>
    /// <returns>The decode result.</returns>
    public static SKBitmap Decode(Stream content)
    {
        var info = new MagickImageInfo(content);
        if (info.Format != MagickFormat.Avif || info.Width == 0 || info.Height == 0 ||
            (long)info.Width * info.Height > 40_000_000)
        {
            throw new InvalidDataException("The AVIF dimensions or format are invalid.");
        }

        content.Position = 0;
        using var image = new MagickImage(content, MagickFormat.Avif);
        return SKBitmap.Decode(image.ToByteArray(MagickFormat.Png))
            ?? throw new InvalidDataException("The AVIF image cannot be decoded.");
    }

    /// <summary>Performs the has signature operation.</summary>
    /// <param name="content">The content supplied to this operation.</param>
    /// <returns>The has signature result.</returns>
    public static bool HasSignature(Stream content)
    {
        Span<byte> header = stackalloc byte[32];
        var count = content.Read(header);
        content.Position = 0;
        return count >= 16 && header[4..8].SequenceEqual("ftyp"u8) &&
            (header[8..count].IndexOf("avif"u8) >= 0 || header[8..count].IndexOf("avis"u8) >= 0);
    }
}
