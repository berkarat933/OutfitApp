namespace OutfitApp.Core.Entities;

public class ClothingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string? Color { get; set; }
    public string? SecondaryColor { get; set; }
    public string Season { get; set; } = "AllSeasons";
    public string Material { get; set; } = "Unknown";
    public string Pattern { get; set; } = "Solid";
    public string Style { get; set; } = "Casual";
    public string Fit { get; set; } = "Unknown";
    public string Occasion { get; set; } = "Everyday";
    public string? Brand { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();
}
