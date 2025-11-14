
namespace MineGuess.Api.Models;

public class Dimension
{
    public int Id { get; set; }
    public string Key { get; set; } = "";  // overworld|nether|end
    public string Name { get; set; } = "";
}

public class Biome
{
    public int Id { get; set; }
    public string Key { get; set; } = "";  // plains, desert, warped_forest, etc.
    public string Name { get; set; } = "";
}
