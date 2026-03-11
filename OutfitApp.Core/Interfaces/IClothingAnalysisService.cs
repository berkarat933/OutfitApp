using OutfitApp.Core.DTOs;

namespace OutfitApp.Core.Interfaces;

public interface IClothingAnalysisService
{
    Task<ClothingAnalysisResult> AnalyzeClothingImageAsync(string imageUrl);
}
