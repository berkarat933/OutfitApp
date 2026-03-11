using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Entities;
using OutfitApp.Core.Interfaces;
using OutfitApp.Infrastructure.Data;

namespace OutfitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WardrobeController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IClothingAnalysisService _analysisService;
    private readonly OutfitAppDbContext _db;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    // Geçici olarak sabit bir UserId kullanıyoruz (auth yokken)
    private static readonly Guid TempUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public WardrobeController(
        IBlobStorageService blobStorageService,
        IClothingAnalysisService analysisService,
        OutfitAppDbContext db)
    {
        _blobStorageService = blobStorageService;
        _analysisService = analysisService;
        _db = db;
    }

    /// <summary>
    /// Step 1: Upload photo → Blob + AI analysis → Returns suggestions (does NOT save to DB)
    /// </summary>
    [HttpPost("analyze")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        // 1. Validate
        if (file.Length == 0)
            return BadRequest(new { error = "File cannot be empty." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest(new { error = $"Invalid file format. Allowed: {string.Join(", ", _allowedExtensions)}" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "File size cannot exceed 10 MB." });

        // 1. Upload to Blob Storage
        await using var stream = file.OpenReadStream();
        var blobResult = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);

        // 2. AI Analysis (GPT-4o Vision)
        ClothingAnalysisResult analysis;
        try
        {
            analysis = await _analysisService.AnalyzeClothingImageAsync(blobResult.BlobUrl);
        }
        catch (InvalidOperationException ex)
        {
            // Not a clothing image — delete the uploaded blob and return error
            await _blobStorageService.DeleteAsync(blobResult.FileName);
            return BadRequest(new { error = ex.Message });
        }

        // 3. Return AI suggestions + imageUrl (user will edit and then save)
        return Ok(new
        {
            imageUrl = blobResult.BlobUrl,
            blobFileName = blobResult.FileName,
            suggestion = new
            {
                name = analysis.Name,
                category = analysis.Category.ToString(),
                color = analysis.Color,
                secondaryColor = analysis.SecondaryColor,
                season = analysis.Season.ToString(),
                material = analysis.Material.ToString(),
                pattern = analysis.Pattern.ToString(),
                style = analysis.Style.ToString(),
                fit = analysis.Fit.ToString(),
                occasion = analysis.Occasion.ToString(),
                brand = analysis.Brand
            }
        });
    }

    /// <summary>
    /// Step 2: Save the user-edited clothing item to database
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveClothingRequest request)
    {
        await EnsureTempUserExists();

        var clothingItem = new ClothingItem
        {
            Id = Guid.NewGuid(),
            UserId = TempUserId,
            Name = request.Name,
            Category = request.Category,
            Color = request.Color,
            SecondaryColor = request.SecondaryColor,
            Season = request.Season,
            Material = request.Material,
            Pattern = request.Pattern,
            Style = request.Style,
            Fit = request.Fit,
            Occasion = request.Occasion,
            Brand = request.Brand,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.ClothingItems.Add(clothingItem);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = clothingItem.Id,
            name = clothingItem.Name,
            category = clothingItem.Category,
            color = clothingItem.Color,
            secondaryColor = clothingItem.SecondaryColor,
            season = clothingItem.Season,
            material = clothingItem.Material,
            pattern = clothingItem.Pattern,
            style = clothingItem.Style,
            fit = clothingItem.Fit,
            occasion = clothingItem.Occasion,
            brand = clothingItem.Brand,
            imageUrl = clothingItem.ImageUrl,
            createdAt = clothingItem.CreatedAt
        });
    }

    /// <summary>
    /// Cancel an analyzed image (delete blob without saving to DB)
    /// </summary>
    [HttpDelete("cancel/{blobFileName}")]
    public async Task<IActionResult> CancelUpload(string blobFileName)
    {
        await _blobStorageService.DeleteAsync(blobFileName);
        return Ok(new { message = "Upload cancelled." });
    }

    /// <summary>
    /// Get all clothing items in the wardrobe
    /// </summary>
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? category = null,
        [FromQuery] string? season = null,
        [FromQuery] string? style = null,
        [FromQuery] string? occasion = null)
    {
        var query = _db.ClothingItems
            .Where(c => c.UserId == TempUserId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(c => c.Category.Contains(category));

        if (!string.IsNullOrEmpty(season))
            query = query.Where(c => c.Season.Contains(season));

        if (!string.IsNullOrEmpty(style))
            query = query.Where(c => c.Style.Contains(style));

        if (!string.IsNullOrEmpty(occasion))
            query = query.Where(c => c.Occasion.Contains(occasion));

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                category = c.Category,
                color = c.Color,
                secondaryColor = c.SecondaryColor,
                season = c.Season,
                material = c.Material,
                pattern = c.Pattern,
                style = c.Style,
                fit = c.Fit,
                occasion = c.Occasion,
                brand = c.Brand,
                imageUrl = c.ImageUrl,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Get a single clothing item by ID
    /// </summary>
    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        var item = await _db.ClothingItems
            .Where(c => c.Id == id && c.UserId == TempUserId)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                category = c.Category,
                color = c.Color,
                secondaryColor = c.SecondaryColor,
                season = c.Season,
                material = c.Material,
                pattern = c.Pattern,
                style = c.Style,
                fit = c.Fit,
                occasion = c.Occasion,
                brand = c.Brand,
                imageUrl = c.ImageUrl,
                createdAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { error = "Clothing item not found." });

        return Ok(item);
    }

    /// <summary>
    /// Delete a clothing item (removes from DB and Blob Storage)
    /// </summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _db.ClothingItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == TempUserId);

        if (item == null)
            return NotFound(new { error = "Clothing item not found." });

        // Extract blob file name from URL
        var uri = new Uri(item.ImageUrl);
        var blobFileName = Path.GetFileName(uri.LocalPath);
        await _blobStorageService.DeleteAsync(blobFileName);

        _db.ClothingItems.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Clothing item deleted.", id });
    }

    private async Task EnsureTempUserExists()
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == TempUserId);
        if (!exists)
        {
            _db.Users.Add(new User
            {
                Id = TempUserId,
                Email = "temp@outfitapp.com",
                DisplayName = "Test User",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Request model for saving a clothing item after user edits AI suggestions
/// </summary>
public class SaveClothingRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? SecondaryColor { get; set; }
    public string Season { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;
    public string Occasion { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
