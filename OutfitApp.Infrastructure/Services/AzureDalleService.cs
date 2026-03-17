using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.Infrastructure.Services;

public class AzureDalleService : IOutfitImageService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;

    public AzureDalleService(string endpoint, string apiKey, string deploymentName = "dall-e-3")
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateOutfitImageAsync(string outfitDescription)
    {
        // Azure OpenAI DALL-E 3 API
        var requestUrl = $"{_endpoint}/openai/deployments/{_deploymentName}/images/generations?api-version=2024-02-01";

        var requestBody = new
        {
            prompt = outfitDescription,
            n = 1,
            size = "1024x1024",
            quality = "standard",
            style = "natural"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("api-key", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DALL-E image generation failed: {response.StatusCode} - {responseContent}");
        }

        // Parse response to get image URL
        using var doc = JsonDocument.Parse(responseContent);
        var imageUrl = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("url")
            .GetString();

        return imageUrl ?? throw new InvalidOperationException("No image URL in response");
    }
}
