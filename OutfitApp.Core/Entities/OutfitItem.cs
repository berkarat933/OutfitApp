namespace OutfitApp.Core.Entities;

public class OutfitItem
{
    public Guid OutfitId { get; set; }
    public Guid ClothingItemId { get; set; }

    // Navigation properties
    public Outfit Outfit { get; set; } = null!;
    public ClothingItem ClothingItem { get; set; } = null!;
}
