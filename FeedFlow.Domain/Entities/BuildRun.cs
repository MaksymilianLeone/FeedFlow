using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Domain
{
    public class BuildRun
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FeedId { get; set; }
        public Feed Feed { get; set; } = default!;

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndedAt { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Running";

        public string? ErrorsJson { get; set; }
    }
}
