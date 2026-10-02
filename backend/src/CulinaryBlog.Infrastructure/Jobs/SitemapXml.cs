using System.Text;
using System.Xml;

namespace CulinaryBlog.Infrastructure.Jobs;

internal sealed record SitemapEntry(string PathAndQuery, DateTimeOffset? LastModified = null);

internal static class SitemapXml
{
    public static byte[] Build(Uri publicBaseUrl, IReadOnlyCollection<SitemapEntry> entries)
    {
        using var output = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Async = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
        };

        using (var writer = XmlWriter.Create(output, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var entry in entries)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", new Uri(publicBaseUrl, entry.PathAndQuery).AbsoluteUri);
                if (entry.LastModified is not null)
                {
                    writer.WriteElementString("lastmod", entry.LastModified.Value.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return output.ToArray();
    }
}
