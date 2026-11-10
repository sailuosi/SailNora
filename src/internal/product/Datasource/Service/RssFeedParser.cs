using System.Xml.Linq;

namespace SailNora.Product.Datasource.Service;

public sealed record RssFeedItem(string Guid, string Title, string Link, string Content);

public static class RssFeedParser
{
    public static List<RssFeedItem> Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var items = new List<RssFeedItem>();

        // RSS 2.0
        foreach (var item in doc.Descendants("item"))
        {
            var title = (string?)item.Element("title") ?? "(untitled)";
            var link = (string?)item.Element("link") ?? string.Empty;
            var guid = (string?)item.Element("guid") ?? link;
            if (string.IsNullOrWhiteSpace(guid))
                guid = title;
            var description = (string?)item.Element("description")
                              ?? (string?)item.Element("content")
                              ?? string.Empty;
            items.Add(new RssFeedItem(guid.Trim(), title.Trim(), link.Trim(), description.Trim()));
        }

        if (items.Count > 0)
            return items;

        // Atom
        XNamespace atom = "http://www.w3.org/2005/Atom";
        foreach (var entry in doc.Descendants(atom + "entry"))
        {
            var title = (string?)entry.Element(atom + "title") ?? "(untitled)";
            var link = entry.Elements(atom + "link")
                           .Select(l => (string?)l.Attribute("href"))
                           .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h))
                       ?? string.Empty;
            var id = (string?)entry.Element(atom + "id") ?? link;
            if (string.IsNullOrWhiteSpace(id))
                id = title;
            var summary = (string?)entry.Element(atom + "summary")
                          ?? (string?)entry.Element(atom + "content")
                          ?? string.Empty;
            items.Add(new RssFeedItem(id.Trim(), title.Trim(), link.Trim(), summary.Trim()));
        }

        return items;
    }
}
