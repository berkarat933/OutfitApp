using OutfitApp.Core.Enums;

namespace OutfitApp.Core.Entities;

public class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Occasion Occasion { get; set; } = Occasion.Casual;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();
}
