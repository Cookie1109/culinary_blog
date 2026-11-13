// Culinary Blog. Authorship is recorded in the repository history.

using System.Text;
using System.Xml;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the sitemap xml.</summary>
internal static class SitemapXml
{
    /// <summary>Builds the sitemap xml.</summary>
    /// <param name="publicBaseUrl">The public base url supplied to this operation.</param>
    /// <param name="entries">The entries supplied to this operation.</param>
    /// <returns>The build result.</returns>
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

                writer.WriteElementString("changefreq", entry.PathAndQuery.StartsWith("/recipes/", StringComparison.Ordinal) ? "weekly" : "daily");
                writer.WriteElementString("priority", entry.PathAndQuery == "/" ? "1.0" : "0.8");

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return output.ToArray();
    }
}
