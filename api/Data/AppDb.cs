using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Models;

namespace MineGuess.Api.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<GameVersion> GameVersions => Set<GameVersion>();
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<Biome> Biomes => Set<Biome>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<BlockBiome> BlockBiomes => Set<BlockBiome>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<EntityDimension> EntityDimensions => Set<EntityDimension>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Block>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Block>().HasIndex(x => x.Name);
        b.Entity<Entity>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Entity>().HasIndex(x => x.Name);
        b.Entity<Dimension>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Biome>().HasIndex(x => x.Key).IsUnique();
        b.Entity<GameVersion>().HasIndex(x => x.OrderKey);

        b.Entity<BlockBiome>().HasKey(x => new { x.BlockId, x.BiomeId });
        b.Entity<BlockBiome>()
            .HasOne(x => x.Block)
            .WithMany(bk => bk.BlockBiomes)
            .HasForeignKey(x => x.BlockId);

        b.Entity<BlockBiome>()
            .HasOne(x => x.Biome)
            .WithMany(bm => bm.BlockBiomes)
            .HasForeignKey(x => x.BiomeId);

        b.Entity<EntityDimension>().HasKey(x => new { x.EntityId, x.DimensionId });
        b.Entity<EntityDimension>()
            .HasOne(x => x.Entity)
            .WithMany(e => e.EntityDimensions)
            .HasForeignKey(x => x.EntityId);

        b.Entity<EntityDimension>()

            .HasOne(x => x.Dimension)
            .WithMany(d => d.EntityDimensions)
            .HasForeignKey(x => x.DimensionId);

        base.OnModelCreating(b);
    }
}
