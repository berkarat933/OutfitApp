using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitApp.Core.Entities;
using OutfitApp.Core.Interfaces;
using OutfitApp.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace OutfitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OutfitController : ControllerBase
{
    private readonly IOutfitImageService _outfitImageService;
    private readonly IClothingAnalysisService _analysisService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly OutfitAppDbContext _db;
    private readonly ILogger<OutfitController> _logger;
    private readonly IConfiguration _configuration;

    public OutfitController(
        IOutfitImageService outfitImageService,
        IClothingAnalysisService analysisService,
        IBlobStorageService blobStorageService,
        OutfitAppDbContext db,
        ILogger<OutfitController> logger,
        IConfiguration configuration)
    {
        _outfitImageService = outfitImageService;
        _analysisService = analysisService;
        _blobStorageService = blobStorageService;
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get or create user from Auth0 token
    /// </summary>
    private async Task<User> GetOrCreateUserAsync()
    {
        var auth0SubjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub") 
            ?? throw new UnauthorizedAccessException("User not authenticated");
        
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "";
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "User";
        var picture = User.FindFirstValue("picture");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Auth0SubjectId == auth0SubjectId);
        
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Auth0SubjectId = auth0SubjectId,
                Email = email,
                DisplayName = name,
                AvatarUrl = picture,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return user;
    }

    /// <summary>
    /// Generate a stylized outfit image from selected clothing items
    /// </summary>
    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateOutfitImage([FromBody] GenerateOutfitRequest request)
    {
        var user = await GetOrCreateUserAsync();
        
        // Get clothing items from database
        var clothingItems = await _db.ClothingItems
            .Where(c => request.ClothingItemIds.Contains(c.Id) && c.UserId == user.Id)
            .ToListAsync();

        if (clothingItems.Count == 0)
            return BadRequest(new { error = "No clothing items found." });

        // Build outfit description from clothing metadata
        var itemDescriptions = clothingItems.Select(c =>
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(c.Color)) parts.Add(c.Color.ToLower());
            if (!string.IsNullOrEmpty(c.Pattern) && c.Pattern != "Solid") parts.Add(c.Pattern.ToLower());
            if (!string.IsNullOrEmpty(c.Material) && c.Material != "Unknown") parts.Add(c.Material.ToLower());
            if (!string.IsNullOrEmpty(c.Fit) && c.Fit != "Unknown") parts.Add(c.Fit.ToLower().Replace("fit", "-fit"));
            parts.Add(c.Category.ToLower());
            return string.Join(" ", parts);
        }).ToList();

        var outfitStyle = clothingItems.FirstOrDefault()?.Style ?? "Casual";
        var gender = request.Gender ?? "person";

        // Create detailed prompt for DALL-E 3
        var prompt = $"A stylish {gender} wearing {string.Join(", ", itemDescriptions)}. " +
                     $"{outfitStyle} style, full body shot, fashion photography, " +
                     $"studio lighting, clean white background, high quality, professional photo.";

        _logger.LogInformation("Generating outfit image with prompt: {Prompt}", prompt);

        try
        {
            var imageUrl = await _outfitImageService.GenerateOutfitImageAsync(prompt);

            return Ok(new
            {
                imageUrl = imageUrl,
                prompt = prompt,
                items = clothingItems.Select(c => new { c.Id, c.Name, c.Category, c.Color })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate outfit image");
            return StatusCode(500, new { error = "Failed to generate outfit image: " + ex.Message });
        }
    }

    /// <summary>
    /// Get AI-powered outfit suggestions based on wardrobe
    /// </summary>
    [HttpPost("suggest")]
    public async Task<IActionResult> SuggestOutfit([FromBody] SuggestOutfitRequest request)
    {
        var user = await GetOrCreateUserAsync();
        
        // Get all user's clothing items
        var allItems = await _db.ClothingItems
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (allItems.Count < 2)
            return BadRequest(new { error = "You need at least 2 clothing items to create an outfit." });

        try
        {
            // Build wardrobe description for AI
            var wardrobeJson = JsonSerializer.Serialize(allItems.Select(c => new
            {
                id = c.Id.ToString(),
                name = c.Name,
                category = c.Category,
                color = c.Color,
                secondaryColor = c.SecondaryColor,
                season = c.Season,
                material = c.Material,
                pattern = c.Pattern,
                style = c.Style,
                fit = c.Fit,
                occasion = c.Occasion
            }));

            var occasion = request.Occasion ?? "everyday";
            var season = request.Season ?? "AllSeasons";
            var style = request.Style ?? "any";

            // Call GPT-4o for outfit suggestions
            var suggestions = await GetAIOutfitSuggestions(wardrobeJson, occasion, season, style);

            // Map AI suggestions to actual items with image URLs
            var result = new List<object>();
            foreach (var suggestion in suggestions)
            {
                var outfitItems = new List<object>();
                foreach (var itemId in suggestion.ItemIds)
                {
                    var item = allItems.FirstOrDefault(i => i.Id.ToString() == itemId);
                    if (item != null)
                    {
                        outfitItems.Add(new
                        {
                            id = item.Id,
                            name = item.Name,
                            category = item.Category,
                            color = item.Color,
                            imageUrl = _blobStorageService.GetBlobUrl(item.ImageUrl)
                        });
                    }
                }

                if (outfitItems.Count >= 2)
                {
                    result.Add(new
                    {
                        name = suggestion.Name,
                        description = suggestion.Description,
                        occasion = suggestion.Occasion,
                        items = outfitItems
                    });
                }
            }

            return Ok(new { suggestions = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI outfit suggestions");
            return StatusCode(500, new { error = "Failed to get outfit suggestions: " + ex.Message });
        }
    }

    private async Task<List<OutfitSuggestion>> GetAIOutfitSuggestions(string wardrobeJson, string occasion, string season, string style)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/');
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

        var requestUrl = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2025-01-01-preview";

        var systemPrompt = """
            You are a fashion stylist AI. Given a wardrobe of clothing items, suggest 2-3 outfit combinations.
            
            Rules:
            1. Each outfit MUST have at least one top (TShirt, Shirt, Sweater, Blouse, Hoodie, Polo) AND one bottom (Jeans, Trousers, Shorts, Skirt)
            2. Optionally add shoes, jacket, or accessories if available
            3. Colors should complement each other
            4. Style and occasion should match
            5. Season should be appropriate
            
            Return a JSON array with this format:
            [
              {
                "name": "Casual Friday",
                "description": "A relaxed yet stylish look perfect for casual occasions",
                "occasion": "casual",
                "itemIds": ["id1", "id2", "id3"]
              }
            ]
            
            ONLY return valid JSON array, no other text.
            """;

        var userPrompt = $"""
            Wardrobe items:
            {wardrobeJson}
            
            Create outfit suggestions for:
            - Occasion: {occasion}
            - Season: {season}
            - Preferred style: {style}
            """;

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API error: {Response}", responseContent);
            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // Clean up response (remove markdown code blocks if present)
        content = content?.Trim();
        if (content?.StartsWith("```") == true)
        {
            content = content.Substring(content.IndexOf('\n') + 1);
            content = content.Substring(0, content.LastIndexOf("```"));
        }

        var suggestions = JsonSerializer.Deserialize<List<OutfitSuggestion>>(content!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return suggestions ?? new List<OutfitSuggestion>();
    }

    /// <summary>
    /// Get all wardrobe items for outfit selection
    /// </summary>
    [HttpGet("wardrobe-items")]
    public async Task<IActionResult> GetWardrobeItems()
    {
        var user = await GetOrCreateUserAsync();
        
        var items = await _db.ClothingItems
            .Where(c => c.UserId == user.Id)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                category = c.Category,
                color = c.Color,
                imageUrl = c.ImageUrl
            })
            .ToListAsync();

        // Add blob URLs
        var result = items.Select(i => new
        {
            i.id,
            i.name,
            i.category,
            i.color,
            imageUrl = string.IsNullOrEmpty(i.imageUrl) ? null : _blobStorageService.GetBlobUrl(i.imageUrl)
        });

        return Ok(result);
    }
}

public class OutfitSuggestion
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Occasion { get; set; } = string.Empty;
    public List<string> ItemIds { get; set; } = new();
}

public class GenerateOutfitRequest
{
    public List<Guid> ClothingItemIds { get; set; } = new();
    public string? Gender { get; set; } // "man", "woman", "person"
}

public class SuggestOutfitRequest
{
    public string? Occasion { get; set; }
    public string? Season { get; set; }
    public string? Style { get; set; }
}
