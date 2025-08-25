using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Domain
{
    public class Feed
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrgId { get; set; }
        public Org Org { get; set; } = default!;

        [MaxLength(80)]
        public string Name { get; set; } = "Google Merchant";

        [MaxLength(40)]
        public string Channel { get; set; } = "google-merchant";

        [MaxLength(2048)]
        public string? PublicUrl { get; set; }

        public DateTimeOffset? LastBuiltAt { get; set; }
    }
}
