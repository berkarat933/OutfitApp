using Microsoft.AspNetCore.Mvc;
using OutfitApp.Core.DTOs;
using OutfitApp.Core.Interfaces;

namespace OutfitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImageController(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Upload an image to Azure Blob Storage
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<BlobUploadResult>> Upload(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("Dosya boş olamaz.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest($"Geçersiz dosya formatı. İzin verilen formatlar: {string.Join(", ", _allowedExtensions)}");

        if (file.Length > MaxFileSize)
            return BadRequest("Dosya boyutu 10 MB'dan büyük olamaz.");

        await using var stream = file.OpenReadStream();
        var result = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);

        return Ok(result);
    }

    /// <summary>
    /// Upload multiple images to Azure Blob Storage
    /// </summary>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(MaxFileSize * 5)]
    public async Task<ActionResult<List<BlobUploadResult>>> UploadMultiple(List<IFormFile> files)
    {
        if (files.Count == 0)
            return BadRequest("En az bir dosya seçmelisiniz.");

        var results = new List<BlobUploadResult>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                continue;

            await using var stream = file.OpenReadStream();
            var result = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);
            results.Add(result);
        }

        return Ok(results);
    }

    /// <summary>
    /// List all uploaded images
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<string>>> List()
    {
        var blobs = await _blobStorageService.ListBlobsAsync();
        return Ok(blobs);
    }

    /// <summary>
    /// Delete an image by file name
    /// </summary>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        var deleted = await _blobStorageService.DeleteAsync(fileName);
        if (!deleted)
            return NotFound("Dosya bulunamadı.");

        return Ok(new { message = "Dosya silindi.", fileName });
    }
}
