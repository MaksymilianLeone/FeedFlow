using System.Globalization;
using System.Xml.Linq;
using FeedFlow.Domain;
using FeedFlow.Infrastructure.Storage;

namespace FeedFlow.Infrastructure.Feeds
{
    public class GoogleMerchantFeedBuilder
    {
        private readonly IFileStorage _storage;
        public GoogleMerchantFeedBuilder(IFileStorage storage) => _storage = storage;

        /// <summary>
        /// Builds a Google Merchant XML feed for the given org.
        /// </summary>
        /// <param name="org">Organization owning the products.</param>
        /// <param name="products">Products to include.</param>
        /// <param name="storeName">Channel title.</param>
        /// <param name="utmSource">Optional UTM source.</param>
        /// <param name="utmMedium">Optional UTM medium.</param>
        /// <param name="utmCampaign">Optional UTM campaign.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<string> BuildAsync(
            Org org,
            IEnumerable<Product> products,
            string storeName,
            string? utmSource = null,
            string? utmMedium = null,
            string? utmCampaign = null,
            CancellationToken ct = default)
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
                                new XElement("link", WithUtm(p.Url, utmSource, utmMedium, utmCampaign)),
                                new XElement(g + "price", $"{p.Price.ToString("F2", CultureInfo.InvariantCulture)} {p.Currency}"),
                                p.SalePrice.HasValue
                                    ? new XElement(g + "sale_price", $"{p.SalePrice.Value.ToString("F2", CultureInfo.InvariantCulture)} {p.Currency}")
                                    : null,
                                new XElement(g + "availability", p.Stock > 0 ? "in stock" : "out of stock"),
                                !string.IsNullOrWhiteSpace(p.Brand) ? new XElement(g + "brand", p.Brand) : null,
                                !string.IsNullOrWhiteSpace(p.Gtin) ? new XElement(g + "gtin", p.Gtin) : null,
                                !string.IsNullOrWhiteSpace(p.Mpn) ? new XElement(g + "mpn", p.Mpn) : null,
                                (string.IsNullOrWhiteSpace(p.Gtin) && string.IsNullOrWhiteSpace(p.Mpn))
                                    ? new XElement(g + "identifier_exists", "false")
                                    : null,
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

        private static string WithUtm(string url, string? src, string? med, string? camp)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var q = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(src)) q.Add($"utm_source={Uri.EscapeDataString(src)}");
            if (!string.IsNullOrWhiteSpace(med)) q.Add($"utm_medium={Uri.EscapeDataString(med)}");
            if (!string.IsNullOrWhiteSpace(camp)) q.Add($"utm_campaign={Uri.EscapeDataString(camp)}");
            if (q.Count == 0) return url;

            var hasQ = url.Contains('?', StringComparison.Ordinal);
            var sep = hasQ ? '&' : '?';
            return url + sep + string.Join('&', q);
        }

        private static string StripHtml(string s) =>
            string.IsNullOrEmpty(s) ? s : System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", " ").Trim();

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
    }
}
