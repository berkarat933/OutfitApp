using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Entities;
using OutfitApp.Core.Interfaces;
using OutfitApp.Infrastructure.Data;

namespace OutfitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WardrobeController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IClothingAnalysisService _analysisService;
    private readonly OutfitAppDbContext _db;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

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
        else if (user.Email != email || user.DisplayName != name)
        {
            // Update user info if changed
            user.Email = email;
            user.DisplayName = name;
            user.AvatarUrl = picture;
            await _db.SaveChangesAsync();
        }

        return user;
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
        var user = await GetOrCreateUserAsync();

        // Use BlobFileName if provided, otherwise extract from ImageUrl
        var blobName = request.BlobFileName;
        if (string.IsNullOrEmpty(blobName) && !string.IsNullOrEmpty(request.ImageUrl))
        {
            try
            {
                var uri = new Uri(request.ImageUrl);
                blobName = Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                blobName = request.ImageUrl;
            }
        }

        var clothingItem = new ClothingItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
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
            ImageUrl = blobName ?? "",  // Store blob name, not full URL
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
            imageUrl = _blobStorageService.GetBlobUrl(clothingItem.ImageUrl),
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
    /// Bulk upload: Upload multiple photos, analyze each with AI, and save all to DB automatically
    /// </summary>
    [HttpPost("bulk-upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB total
    public async Task<IActionResult> BulkUpload([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No files provided." });

        var user = await GetOrCreateUserAsync();

        var results = new List<object>();
        var errors = new List<object>();

        foreach (var file in files)
        {
            try
            {
                // Validate file
                if (file.Length == 0)
                {
                    errors.Add(new { fileName = file.FileName, error = "File is empty." });
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    errors.Add(new { fileName = file.FileName, error = "Invalid file format." });
                    continue;
                }

                if (file.Length > MaxFileSize)
                {
                    errors.Add(new { fileName = file.FileName, error = "File too large (max 10 MB)." });
                    continue;
                }

                // Upload to Blob Storage
                await using var stream = file.OpenReadStream();
                var blobResult = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);

                // AI Analysis
                ClothingAnalysisResult analysis;
                try
                {
                    analysis = await _analysisService.AnalyzeClothingImageAsync(blobResult.BlobUrl);
                }
                catch (InvalidOperationException ex)
                {
                    // Not a clothing image — delete blob and skip
                    await _blobStorageService.DeleteAsync(blobResult.FileName);
                    errors.Add(new { fileName = file.FileName, error = ex.Message });
                    continue;
                }

                // Save to database
                var clothingItem = new ClothingItem
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = analysis.Name,
                    Category = analysis.Category,
                    Color = analysis.Color,
                    SecondaryColor = analysis.SecondaryColor,
                    Season = analysis.Season,
                    Material = analysis.Material,
                    Pattern = analysis.Pattern,
                    Style = analysis.Style,
                    Fit = analysis.Fit,
                    Occasion = analysis.Occasion,
                    Brand = analysis.Brand,
                    ImageUrl = blobResult.FileName,  // Store blob name
                    CreatedAt = DateTime.UtcNow
                };

                _db.ClothingItems.Add(clothingItem);
                await _db.SaveChangesAsync();

                results.Add(new
                {
                    id = clothingItem.Id,
                    fileName = file.FileName,
                    name = clothingItem.Name,
                    category = clothingItem.Category,
                    color = clothingItem.Color,
                    imageUrl = _blobStorageService.GetBlobUrl(clothingItem.ImageUrl)
                });
            }
            catch (Exception ex)
            {
                errors.Add(new { fileName = file.FileName, error = ex.Message });
            }
        }

        return Ok(new
        {
            success = results.Count,
            failed = errors.Count,
            total = files.Count,
            items = results,
            errors = errors
        });
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
        try
        {
            var user = await GetOrCreateUserAsync();
            
            var query = _db.ClothingItems
                .Where(c => c.UserId == user.Id)
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
            .ToListAsync();

        // Generate fresh SAS URLs for each item
        var result = items.Select(c => new
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
            imageUrl = _blobStorageService.GetBlobUrl(c.ImageUrl),
            createdAt = c.CreatedAt
        });

        return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get a single clothing item by ID
    /// </summary>
    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        var user = await GetOrCreateUserAsync();
        
        var item = await _db.ClothingItems
            .Where(c => c.Id == id && c.UserId == user.Id)
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { error = "Clothing item not found." });

        return Ok(new
        {
            id = item.Id,
            name = item.Name,
            category = item.Category,
            color = item.Color,
            secondaryColor = item.SecondaryColor,
            season = item.Season,
            material = item.Material,
            pattern = item.Pattern,
            style = item.Style,
            fit = item.Fit,
            occasion = item.Occasion,
            brand = item.Brand,
            imageUrl = _blobStorageService.GetBlobUrl(item.ImageUrl),
            createdAt = item.CreatedAt
        });
    }

    /// <summary>
    /// Delete a clothing item (removes from DB and Blob Storage)
    /// </summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var user = await GetOrCreateUserAsync();
        
        var item = await _db.ClothingItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

        if (item == null)
            return NotFound(new { error = "Clothing item not found." });

        // ImageUrl now stores the blob name directly
        await _blobStorageService.DeleteAsync(item.ImageUrl);

        _db.ClothingItems.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Clothing item deleted.", id });
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
    public string? BlobFileName { get; set; }  // Blob name for generating fresh SAS URLs
}
