
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
    public DbSet<Translation> Translations => Set<Translation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<BlockBiome>().HasKey(x => new { x.BlockId, x.BiomeId });
        b.Entity<EntityDimension>().HasKey(x => new { x.EntityId, x.DimensionId });

        b.Entity<GameVersion>().HasIndex(x => x.OrderKey);
        b.Entity<Block>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Block>().HasIndex(x => x.Name);
        b.Entity<Entity>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Entity>().HasIndex(x => x.Name);
        b.Entity<Dimension>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Biome>().HasIndex(x => x.Key).IsUnique();

        base.OnModelCreating(b);
    }
}
