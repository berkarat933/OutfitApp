using System.Net.Http.Headers;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.Infrastructure.Services;

public class AzureComputerVisionService : IBackgroundRemovalService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureComputerVisionService(string endpoint, string apiKey)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public Task<byte[]> RemoveBackgroundAsync(string imageUrl)
    {
        // Azure Image Analysis 4.0 Segment API was deprecated on February 4, 2025
        // https://azure.microsoft.com/en-us/updates?id=475779
        // Background removal is temporarily disabled
        // Alternatives: remove.bg API, Photoroom API, or DALL-E inpainting
        throw new NotSupportedException("Background removal temporarily disabled - Azure deprecated this API");
    }
}
