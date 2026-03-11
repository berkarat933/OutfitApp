namespace OutfitApp.Core.DTOs;

public class ClothingAnalysisResult
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
}
