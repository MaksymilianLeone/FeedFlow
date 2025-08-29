using CsvHelper;
using CsvHelper.Configuration;
using FeedFlow.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FeedFlow.Infrastructure.Import.CsvProductImporter;

namespace FeedFlow.Infrastructure.Import
{
    public static class CsvProductImporter
    {
        public class CsvProductRow
        {
            public string Sku { get; set; } = default!;
            public string Title { get; set; } = default!;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public decimal? SalePrice { get; set; }
            public string Currency { get; set; } = "USD";
            public int Stock { get; set; }
            public string Url { get; set; } = default!;
            public string ImageUrl { get; set; } = default!;
            public string? Brand { get; set; }
            public string? Gtin { get; set; }
            public string? Mpn { get; set; }
        }
        public static async Task<List<CsvProductRow>> ParseAsync(Stream csv)
        {
            using var reader = new StreamReader(csv);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => (args.Header ?? string.Empty).Trim().ToLowerInvariant(),
                HeaderValidated = null,   
                MissingFieldFound = null, 
                TrimOptions = TrimOptions.Trim
            };
            using var cr = new CsvReader(reader, config);

            // Optional: explicit map with mpn marked optional (keeps behavior strict but tolerant)
            cr.Context.RegisterClassMap<CsvProductRowMap>();

            var list = new List<CsvProductRow>();
            await foreach (var r in cr.GetRecordsAsync<CsvProductRow>())
                list.Add(r);

            return list;
        }

        public static IEnumerable<string> Validate(CsvProductRow r)
        {
            if (string.IsNullOrWhiteSpace(r.Sku)) yield return "SKU required";
            if (string.IsNullOrWhiteSpace(r.Title)) yield return "Title required";
            if (r.Price <= 0) yield return "Price must be > 0";
            if (string.IsNullOrWhiteSpace(r.Url)) yield return "Product URL required";
            if (string.IsNullOrWhiteSpace(r.ImageUrl)) yield return "Image URL required";
            if (!string.IsNullOrWhiteSpace(r.Currency) && r.Currency.Length != 3) yield return "Currency must be 3 letters";
        }

        public static Product MapToEntity(Guid orgId, CsvProductRow r) => new()
        {
            OrgId = orgId,
            Sku = r.Sku.Trim(),
            Title = r.Title.Trim(),
            Description = r.Description,
            Price = r.Price,
            SalePrice = r.SalePrice,
            Currency = (r.Currency ?? "USD").ToUpperInvariant(),
            Stock = r.Stock,
            Url = r.Url.Trim(),
            ImageUrl = r.ImageUrl.Trim(),
            Brand = r.Brand?.Trim(),
            Gtin = r.Gtin?.Trim(),
            Mpn = r.Mpn?.Trim()
        };
    }
    public sealed class CsvProductRowMap : ClassMap<CsvProductRow>
    {
        public CsvProductRowMap()
        {
            Map(m => m.Sku).Name("sku");
            Map(m => m.Title).Name("title");
            Map(m => m.Description).Name("description");
            Map(m => m.Price).Name("price");
            Map(m => m.SalePrice).Name("saleprice");
            Map(m => m.Currency).Name("currency");
            Map(m => m.Stock).Name("stock");
            Map(m => m.Url).Name("url");
            Map(m => m.ImageUrl).Name("imageurl");
            Map(m => m.Brand).Name("brand");
            Map(m => m.Gtin).Name("gtin");
            Map(m => m.Mpn).Name("mpn").Optional();
        }
    }
}
