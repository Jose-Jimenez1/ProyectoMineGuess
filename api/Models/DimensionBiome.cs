namespace MineGuess.Api.Models;

public class Dimension
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";

    public List<Block> Blocks { get; set; } = new();
    public List<EntityDimension> EntityDimensions { get; set; } = new();
}

public class Biome
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";

    public List<BlockBiome> BlockBiomes { get; set; } = new();
}
