
namespace MineGuess.Api.Models;

public class Translation
{
    public int Id { get; set; }
    public string EntityType { get; set; } = ""; // "block", "entity", "biome"
    public int EntityId { get; set; }
    public string Locale { get; set; } = "en-US";
    public string DisplayName { get; set; } = "";
}
