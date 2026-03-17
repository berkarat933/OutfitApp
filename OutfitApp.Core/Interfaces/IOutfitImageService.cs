namespace OutfitApp.Core.Interfaces;

public interface IOutfitImageService
{
    /// <summary>
    /// Generates a stylized outfit image using DALL-E 3
    /// </summary>
    /// <param name="outfitDescription">Description of the outfit (colors, styles, items)</param>
    /// <returns>URL of the generated image</returns>
    Task<string> GenerateOutfitImageAsync(string outfitDescription);
}
