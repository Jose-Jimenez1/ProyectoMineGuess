
using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Data;
using MineGuess.Api.Models;

namespace MineGuess.Api;

public static class SeedUtil
{
    public static async Task SeedAsync(AppDb db, IHostEnvironment env)
    {
        if (await db.GameVersions.AnyAsync()) return;

        // Minimal seed: versions, dimensions, biomes, a few blocks/entities
        var versions = new[] {
            new GameVersion { Name = "1.0", Channel="release", OrderKey = 10000 }
        };
        db.GameVersions.AddRange(versions);

        var dimOver = new Dimension { Key="overworld", Name="Overworld" };
        var dimNether = new Dimension { Key="nether", Name="Nether" };
        var dimEnd = new Dimension { Key="end", Name="The End" };
        db.Dimensions.AddRange(dimOver, dimNether, dimEnd);

        var biomePlains = new Biome { Key="plains", Name="Plains" };
        var biomeDesert = new Biome { Key="desert", Name="Desert" };
        var biomeWarped = new Biome { Key="warped_forest", Name="Warped Forest" };
        db.Biomes.AddRange(biomePlains, biomeDesert, biomeWarped);

        await db.SaveChangesAsync();

        // Load JSON seeds if present
        var seedPath = Path.Combine(env.ContentRootPath, "seed");
        Directory.CreateDirectory(seedPath);
        var blocksFile = Path.Combine(seedPath, "blocks.json");
        var entitiesFile = Path.Combine(seedPath, "entities.json");

        if (File.Exists(blocksFile))
        {
            var json = await File.ReadAllTextAsync(blocksFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<BlockSeed>>(json) ?? new();
            foreach (var s in items)
            {
                var v = await db.GameVersions.FirstOrDefaultAsync(x => x.Name == s.addedIn) ?? versions.Last();
                var d = await db.Dimensions.FirstOrDefaultAsync(x => x.Key == s.dimension) ?? dimOver;
                var b = new Block {
                    Key = s.key, Name = s.name, Category = s.category,
                    Hardness = s.hardness, BlastResistance = s.blastResistance,
                    HasGravity = s.hasGravity, LightLevel = s.lightLevel,
                    IsBreakable = s.isBreakable, BestTool = s.bestTool,
                    AddedInVersionId = v.Id, DimensionId = d.Id
                };
                db.Blocks.Add(b);
                await db.SaveChangesAsync();
                foreach (var biomeKey in s.biomes ?? Array.Empty<string>())
                {
                    var biome = await db.Biomes.FirstOrDefaultAsync(x => x.Key == biomeKey);
                    if (biome != null)
                        db.BlockBiomes.Add(new BlockBiome { BlockId = b.Id, BiomeId = biome.Id });
                }
            }
        }
        else
        {
            var v1 = await db.GameVersions.FirstAsync(x => x.Name=="1.0");
            var b = new Block { Key="stone", Name="Stone", Category="basic", DimensionId=dimOver.Id, AddedInVersionId=v1.Id };
            db.Blocks.Add(b);
        }

        if (File.Exists(entitiesFile))
        {
            var json = await File.ReadAllTextAsync(entitiesFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<EntitySeed>>(json) ?? new();
            foreach (var s in items)
            {
                var v = await db.GameVersions.FirstOrDefaultAsync(x => x.Name == s.addedIn) ?? versions.Last();
                var e = new Entity { Key = s.key, Name = s.name, Kind = s.kind, Health = s.health, Attack = s.attack, SpawnRulesJson = s.spawnRules, AddedInVersionId = v.Id };
                db.Entities.Add(e);
                await db.SaveChangesAsync();
                foreach (var dimKey in s.dimensions ?? Array.Empty<string>())
                {
                    var d = await db.Dimensions.FirstOrDefaultAsync(x => x.Key == dimKey);
                    if (d != null) db.EntityDimensions.Add(new EntityDimension { EntityId = e.Id, DimensionId = d.Id });
                }
            }
        }
        else
        {
            var v1 = await db.GameVersions.FirstAsync(x => x.Name=="1.0");
            var z = new Entity { Key="zombie", Name="Zombie", Kind="hostile", Health=20, Attack=3, SpawnRulesJson="{}", AddedInVersionId=v1.Id };
            db.Entities.Add(z);
        }

        await db.SaveChangesAsync();
    }
}
