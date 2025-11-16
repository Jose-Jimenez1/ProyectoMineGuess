namespace MineGuess.Api.Models;

public class Block
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public decimal? Hardness { get; set; }
    public decimal? BlastResistance { get; set; }
    public bool? HasGravity { get; set; }
    public int? LightLevel { get; set; }
    public bool IsBreakable { get; set; } = true;
    public string? BestTool { get; set; }

    public int? DimensionId { get; set; }
    public Dimension? Dimension { get; set; }

    public int? AddedInVersionId { get; set; }
    public GameVersion? AddedInVersion { get; set; }

    public List<BlockBiome> BlockBiomes { get; set; } = new();
}

public class BlockBiome
{
    public int BlockId { get; set; }
    public Block? Block { get; set; }
    public int BiomeId { get; set; }
    public Biome? Biome { get; set; }
}
