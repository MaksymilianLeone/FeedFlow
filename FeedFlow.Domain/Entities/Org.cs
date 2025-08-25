using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Domain
{
    public class Org
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(160)]
        public string Name { get; set; } = default!;

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Feed> Feeds { get; set; } = new List<Feed>();
    }
}
