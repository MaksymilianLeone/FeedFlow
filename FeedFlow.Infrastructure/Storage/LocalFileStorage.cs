using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedFlow.Infrastructure.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _root;
        public LocalFileStorage(string root) => _root = root;

        public async Task<string> SaveAsync(string path, Stream content, string contentType, bool publicRead = true, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using var fs = File.Create(fullPath);
            await content.CopyToAsync(fs, ct);
            return "/feeds/" + path.Replace("\\", "/");
        }
    }
}
