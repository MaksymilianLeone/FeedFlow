using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Infrastructure.Storage
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(string path, Stream content, string contentType, bool publicRead = true, CancellationToken ct = default);
    }
}
