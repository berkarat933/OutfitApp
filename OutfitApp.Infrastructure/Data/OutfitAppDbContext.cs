using Microsoft.EntityFrameworkCore;
using OutfitApp.Core.Entities;

namespace OutfitApp.Infrastructure.Data;

public class OutfitAppDbContext : DbContext
{
    public OutfitAppDbContext(DbContextOptions<OutfitAppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ClothingItem> ClothingItems => Set<ClothingItem>();
    public DbSet<Outfit> Outfits => Set<Outfit>();
    public DbSet<OutfitItem> OutfitItems => Set<OutfitItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // ClothingItem
        modelBuilder.Entity<ClothingItem>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Color).HasMaxLength(50);
            entity.Property(c => c.SecondaryColor).HasMaxLength(50);
            entity.Property(c => c.ImageUrl).HasMaxLength(1000).IsRequired();
            entity.Property(c => c.Brand).HasMaxLength(100);
            entity.Property(c => c.Category).HasMaxLength(200);
            entity.Property(c => c.Season).HasMaxLength(200);
            entity.Property(c => c.Material).HasMaxLength(200);
            entity.Property(c => c.Pattern).HasMaxLength(200);
            entity.Property(c => c.Style).HasMaxLength(200);
            entity.Property(c => c.Fit).HasMaxLength(200);
            entity.Property(c => c.Occasion).HasMaxLength(200);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.ClothingItems)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
        });

        // Outfit
        modelBuilder.Entity<Outfit>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).HasMaxLength(200).IsRequired();
            entity.Property(o => o.Occasion).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Outfits)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.UserId);
        });

        // OutfitItem (many-to-many join table)
        modelBuilder.Entity<OutfitItem>(entity =>
        {
            entity.HasKey(oi => new { oi.OutfitId, oi.ClothingItemId });

            entity.HasOne(oi => oi.Outfit)
                  .WithMany(o => o.OutfitItems)
                  .HasForeignKey(oi => oi.OutfitId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.ClothingItem)
                  .WithMany(c => c.OutfitItems)
                  .HasForeignKey(oi => oi.ClothingItemId)
                  .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycle
        });
    }
}
