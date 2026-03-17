namespace OutfitApp.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Auth0SubjectId { get; set; } = string.Empty;  // Auth0 "sub" claim
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ClothingItem> ClothingItems { get; set; } = new List<ClothingItem>();
    public ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();
}
