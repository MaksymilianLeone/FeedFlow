using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Domain.Entities
{
    public class StoreSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrgId { get; set; }
        public Org Org { get; set; } = default!;

        [MaxLength(160)] public string StoreName { get; set; } = "Demo Store";
        [MaxLength(2048)] public string? BaseUrl { get; set; }
        [MaxLength(3)] public string DefaultCurrency { get; set; } = "USD";

        [MaxLength(100)] public string? UtmSource { get; set; }
        [MaxLength(100)] public string? UtmMedium { get; set; }
        [MaxLength(100)] public string? UtmCampaign { get; set; }
    }
}
