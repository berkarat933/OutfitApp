using OutfitApp.Core.DTOs;

namespace OutfitApp.Core.Interfaces;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAsync(string fileName);
    Task<IEnumerable<string>> ListBlobsAsync();
    string GetBlobUrl(string blobName);
}
