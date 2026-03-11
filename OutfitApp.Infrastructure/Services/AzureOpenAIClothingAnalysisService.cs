using System.Text;
using System.Text.Json;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.Infrastructure.Services;

public class AzureOpenAIClothingAnalysisService : IClothingAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;

    public AzureOpenAIClothingAnalysisService(string endpoint, string apiKey, string deploymentName)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _httpClient = new HttpClient();
    }

    public async Task<ClothingAnalysisResult> AnalyzeClothingImageAsync(string imageUrl)
    {
        var requestUrl = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version=2025-01-01-preview";

        var systemPrompt = """
            You are a clothing analysis AI. First, determine if the image contains a clothing item or a person wearing clothes.
            
            If the image does NOT contain any clothing (e.g. food, animals, cars, landscapes, random objects), 
            return exactly: {"error":"NOT_CLOTHING","message":"Bu fotoğrafta kıyafet tespit edilemedi."}
            
            If the image contains a PERSON, analyze the most prominent/visible clothing item they are wearing.
            
            If the image contains a clothing item (standalone or worn by someone), return a JSON object with ALL of these fields:
            
            - "name": A short descriptive name (e.g. "Blue Denim Jacket", "Black Running Shoes"). Max 50 chars.
            
            - "category": One of: "TShirt", "Shirt", "Blouse", "Sweater", "Hoodie", "Polo", "Cardigan", "Vest", "Jeans", "Trousers", "Shorts", "Skirt", "Leggings", "Sweatpants", "Chinos", "Jacket", "Coat", "Blazer", "Parka", "Puffer", "Trench", "Dress", "Jumpsuit", "Sneakers", "Boots", "Sandals", "Heels", "Loafers", "Hat", "Cap", "Scarf", "Belt", "Sunglasses", "Bag", "Backpack", "Sportswear", "Suit", "Other"
            
            - "color": The dominant/primary color in English (e.g. "Blue", "Black", "Red", "White", "Navy", "Beige", "Olive", "Burgundy", "Cream", "Coral", "Teal", "Mustard", "Charcoal", "Maroon", "Ivory", "Khaki", "Lavender", "Mint", "Peach", "Rust", "Sage", "Tan", "Wine")
            
            - "secondaryColor": Secondary color if exists, null if single color (e.g. "White", null)
            
            - "season": One of: "Spring", "Summer", "Autumn", "Winter", "SpringSummer", "AutumnWinter", "AllSeasons"
            
            - "material": One of: "Cotton", "Polyester", "Denim", "Leather", "Wool", "Silk", "Linen", "Nylon", "Cashmere", "Velvet", "Suede", "Satin", "Fleece", "Canvas", "Knit", "Synthetic", "Mixed", "Unknown"
            
            - "pattern": One of: "Solid", "Striped", "Plaid", "Checkered", "Floral", "Polka", "Graphic", "Camouflage", "Animal", "Geometric", "Abstract", "Paisley", "Embroidered", "Logo", "Other"
            
            - "style": One of: "Casual", "Formal", "Business", "Sporty", "Streetwear", "Bohemian", "Vintage", "Classic", "Minimalist", "Elegant", "Grunge", "Preppy", "Chic", "Athleisure", "Punk", "Romantic", "Military", "Other"
            
            - "fit": One of: "SlimFit", "RegularFit", "Oversized", "Relaxed", "Skinny", "Loose", "Tailored", "Cropped", "Baggy", "Fitted", "Unknown"
            
            - "occasion": One of: "Casual", "Business", "Special", "Sport", "Party", "Wedding", "Date", "Travel", "Beach", "Outdoor", "Home", "Workout", "Interview", "Formal", "Festival", "Everyday"
            
            - "brand": Detected brand name if visible (e.g. "Nike", "Zara", "H&M"), null if not visible
            
            ONLY return the JSON object, nothing else. No markdown, no explanation.
            Example: {"name":"Navy Slim Fit Chinos","category":"Chinos","color":"Navy","secondaryColor":null,"season":"AllSeasons","material":"Cotton","pattern":"Solid","style":"Classic","fit":"SlimFit","occasion":"Business","brand":null}
            """;

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Analyze this clothing item in detail:" },
                        new { type = "image_url", image_url = new { url = imageUrl } }
                    }
                }
            },
            max_tokens = 300,
            temperature = 0.1
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

        var response = await _httpClient.PostAsync(requestUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Azure OpenAI API error: {response.StatusCode} - {responseBody}");
        }

        // Parse the GPT response
        using var doc = JsonDocument.Parse(responseBody);
        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(messageContent))
        {
            throw new Exception("Azure OpenAI returned empty response");
        }

        // Clean up potential markdown wrapping
        messageContent = messageContent.Trim();
        if (messageContent.StartsWith("```"))
        {
            messageContent = messageContent
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }

        var analysisJson = JsonDocument.Parse(messageContent);
        var root = analysisJson.RootElement;

        // Check if it's a non-clothing image
        if (root.TryGetProperty("error", out var errorProp) && errorProp.GetString() == "NOT_CLOTHING")
        {
            var msg = root.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString()
                : "Bu fotoğrafta kıyafet tespit edilemedi.";
            throw new InvalidOperationException(msg);
        }

        return new ClothingAnalysisResult
        {
            Name = root.GetProperty("name").GetString() ?? "Unknown Item",
            Category = root.GetProperty("category").GetString() ?? "Other",
            Color = root.GetProperty("color").GetString() ?? "Unknown",
            SecondaryColor = GetNullableString(root, "secondaryColor"),
            Season = root.GetProperty("season").GetString() ?? "AllSeasons",
            Material = root.GetProperty("material").GetString() ?? "Unknown",
            Pattern = root.GetProperty("pattern").GetString() ?? "Solid",
            Style = root.GetProperty("style").GetString() ?? "Casual",
            Fit = root.GetProperty("fit").GetString() ?? "Unknown",
            Occasion = root.GetProperty("occasion").GetString() ?? "Everyday",
            Brand = GetNullableString(root, "brand")
        };
    }


    private static string? GetNullableString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
            return prop.GetString();
        return null;
    }
}
