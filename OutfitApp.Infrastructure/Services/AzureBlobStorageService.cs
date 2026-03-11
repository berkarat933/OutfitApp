using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<BlobUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
        var blobClient = _containerClient.GetBlobClient(uniqueFileName);

        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        return new BlobUploadResult
        {
            FileName = uniqueFileName,
            BlobUrl = GenerateSasUrl(blobClient),
            FileSize = fileStream.Length,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        var response = await blobClient.DeleteIfExistsAsync();
        return response.Value;
    }

    public async Task<IEnumerable<string>> ListBlobsAsync()
    {
        var blobs = new List<string>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync())
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            blobs.Add(GenerateSasUrl(blobClient));
        }
        return blobs;
    }

    private static string GenerateSasUrl(BlobClient blobClient)
    {
        if (!blobClient.CanGenerateSasUri)
            return blobClient.Uri.ToString();

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(2)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
