using FeedFlow.Domain;
using FeedFlow.Infrastructure.Storage;
using System.Xml.Linq;

namespace FeedFlow.Infrastructure.Feeds
{
    public class GoogleMerchantFeedBuilder
    {
        private readonly IFileStorage _storage;
        public GoogleMerchantFeedBuilder(IFileStorage storage) => _storage = storage;

        public async Task<string> BuildAsync(Org org, IEnumerable<Product> products, string storeName, CancellationToken ct = default)
        {
            XNamespace g = "http://base.google.com/ns/1.0";
            var doc = new XDocument(
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XAttribute(XNamespace.Xmlns + "g", g.NamespaceName),
                    new XElement("channel",
                        new XElement("title", storeName),
                        products.Where(p => p.IsActive).Select(p =>
                            new XElement("item",
                                new XElement(g + "id", p.Sku),
                                new XElement("title", Truncate(p.Title, 150)),
                                new XElement("description", Truncate(StripHtml(p.Description ?? ""), 5000)),
                                new XElement("link", p.Url),
                                new XElement(g + "price", $"{p.Price:F2} {p.Currency}"),
                                p.SalePrice.HasValue ? new XElement(g + "sale_price", $"{p.SalePrice:F2} {p.Currency}") : null,
                                new XElement(g + "availability", p.Stock > 0 ? "in stock" : "out of stock"),
                                !string.IsNullOrWhiteSpace(p.Brand) ? new XElement(g + "brand", p.Brand) : null,
                                !string.IsNullOrWhiteSpace(p.Gtin) ? new XElement(g + "gtin", p.Gtin) : null,
                                new XElement(g + "image_link", p.ImageUrl),
                                new XElement(g + "condition", "new")
                            )
                        )
                    )
                )
            );

            using var ms = new MemoryStream();
            doc.Save(ms);
            ms.Position = 0;
            return await _storage.SaveAsync($"orgs/{org.Id}/google.xml", ms, "application/xml", true, ct);
        }

        private static string StripHtml(string s) =>
            string.IsNullOrEmpty(s) ? s : System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", " ").Trim();

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
    }
}
