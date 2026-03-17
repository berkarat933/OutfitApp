using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly StorageSharedKeyCredential _sharedKeyCredential;
    private readonly string _containerName;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        _containerName = containerName;
        
        // Parse connection string to get account name and key
        var connStringParts = connectionString.Split(';')
            .Select(s => s.Split(new[] { '=' }, 2))
            .Where(s => s.Length == 2)
            .ToDictionary(s => s[0], s => s[1]);
        
        var accountName = connStringParts.GetValueOrDefault("AccountName") ?? "";
        var accountKey = connStringParts.GetValueOrDefault("AccountKey") ?? "";
        
        _sharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
        
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

    public string GetBlobUrl(string blobName)
    {
        if (string.IsNullOrEmpty(blobName))
            return string.Empty;
        
        // If it's already a full URL, extract the blob name
        if (blobName.StartsWith("http"))
        {
            try
            {
                var uri = new Uri(blobName);
                blobName = Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                // If parsing fails, use as-is
            }
        }
            
        var blobClient = _containerClient.GetBlobClient(blobName);
        return GenerateSasUrl(blobClient);
    }

    private string GenerateSasUrl(BlobClient blobClient)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder.ToSasQueryParameters(_sharedKeyCredential).ToString();
        return $"{blobClient.Uri}?{sasToken}";
    }
}
