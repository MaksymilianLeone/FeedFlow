using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Domain
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrgId { get; set; }
        public Org Org { get; set; } = default!;

        [MaxLength(120)]
        public string Sku { get; set; } = default!;

        [MaxLength(150)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [MaxLength(80)]
        public string? Brand { get; set; }

        [MaxLength(50)]
        public string? Gtin { get; set; }

        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        public int Stock { get; set; }

        [MaxLength(2048)]
        public string Url { get; set; } = default!;

        [MaxLength(2048)]
        public string ImageUrl { get; set; } = default!;

        public bool IsActive { get; set; } = true;
    }
}
