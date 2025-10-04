using CsvHelper;
using CsvHelper.Configuration;
using FeedFlow.Domain;
using System.Globalization;
using System.Text;

namespace FeedFlow.Infrastructure.Import
{
    public static class CsvProductImporter
    {
        public sealed class CsvRow
        {
            public string? Sku { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public decimal? Price { get; set; }
            public decimal? SalePrice { get; set; }
            public string? Currency { get; set; }
            public int? Stock { get; set; }
            public string? Availability { get; set; }
            public string? Url { get; set; }
            public string? ImageUrl { get; set; }
            public string? Brand { get; set; }
            public string? Gtin { get; set; }
            public string? Mpn { get; set; }
        }

        static int? ToStockFromAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability)) return null;
            var a = availability.Trim().ToLowerInvariant();
            if (a is "instock" or "in stock" or "available" or "preorder" or "backorder") return 1;
            if (a is "outofstock" or "out of stock" or "unavailable") return 0;
            return null;
        }

        public static async Task<List<CsvRow>> ParseAsync(Stream csv)
        {
            using var reader = new StreamReader(csv, Encoding.UTF8, true, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => (args.Header ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("_", ""),
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };
            using var cr = new CsvReader(reader, config);
            cr.Context.RegisterClassMap<CsvRowMap>();

            var list = new List<CsvRow>();
            await foreach (var r in cr.GetRecordsAsync<CsvRow>())
                list.Add(r);
            return list;
        }

        public static IEnumerable<string> Validate(CsvRow r)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(r.Sku)) errs.Add("SKU required");
            if (string.IsNullOrWhiteSpace(r.Url)) errs.Add("Product URL required");
            if (string.IsNullOrWhiteSpace(r.ImageUrl)) errs.Add("Image URL required");
            if (!r.Price.HasValue) errs.Add("Price required");
            return errs;
        }

        public static Product MapToEntity(Guid orgId, CsvRow r)
        {
            var stock = r.Stock ?? ToStockFromAvailability(r.Availability) ?? 0;
            var currency = string.IsNullOrWhiteSpace(r.Currency) ? "PLN" : r.Currency!.Trim().ToUpperInvariant();

            return new Product
            {
                OrgId = orgId,
                Sku = r.Sku!,
                Title = r.Title?.Trim(),
                Description = r.Description?.Trim(),
                Price = r.Price!.Value,
                SalePrice = r.SalePrice,
                Currency = currency,
                Stock = stock,
                Url = r.Url!,
                ImageUrl = r.ImageUrl!,
                Brand = r.Brand,
                Gtin = r.Gtin,
                Mpn = r.Mpn,
                IsActive = true
            };
        }
    }

    public sealed class CsvRowMap : ClassMap<CsvProductImporter.CsvRow>
    {
        public CsvRowMap()
        {
            Map(m => m.Sku).Name("sku", "id");
            Map(m => m.Title).Name("title");
            Map(m => m.Description).Name("description");
            Map(m => m.Price).Name("price");
            Map(m => m.SalePrice).Name("saleprice", "sale_price");
            Map(m => m.Currency).Name("currency").Optional();
            Map(m => m.Stock).Name("stock", "inventory", "qty", "quantity").Optional();
            Map(m => m.Availability).Name("availability", "stockstatus").Optional();
            Map(m => m.Url).Name("url", "producturl", "product_url", "link", "productlink");
            Map(m => m.ImageUrl).Name("imageurl", "image_url", "imagelink", "image", "imageurlprimary");
            Map(m => m.Brand).Name("brand").Optional();
            Map(m => m.Gtin).Name("gtin", "ean", "barcode", "upc").Optional();
            Map(m => m.Mpn).Name("mpn").Optional();
        }
    }
}
