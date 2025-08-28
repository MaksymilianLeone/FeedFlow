using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FeedFlow.Infrastructure.Storage;

public class AzureBlobStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorage(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> SaveAsync(string path, Stream content, string contentType, bool publicRead = true, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(path.Replace("\\", "/"));
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri.ToString(); // public HTTPS URL
    }
}
