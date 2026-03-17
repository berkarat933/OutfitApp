namespace OutfitApp.Core.Interfaces;

public interface IBackgroundRemovalService
{
    /// <summary>
    /// Removes background from an image and returns the transparent PNG as a byte array
    /// </summary>
    /// <param name="imageUrl">URL of the source image</param>
    /// <returns>Transparent PNG image as byte array</returns>
    Task<byte[]> RemoveBackgroundAsync(string imageUrl);
}
