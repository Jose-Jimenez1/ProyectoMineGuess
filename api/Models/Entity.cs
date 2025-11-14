
namespace MineGuess.Api.Models;

public class Entity
{
    public int Id { get; set; }
    public string Key { get; set; } = "";    // zombie
    public string Name { get; set; } = "";   // Zombie
    public string? Kind { get; set; }        // hostile|passive|neutral|boss
    public int? Health { get; set; }
    public int? Attack { get; set; }
    public string? SpawnRulesJson { get; set; }

    public int? AddedInVersionId { get; set; }
    public GameVersion? AddedInVersion { get; set; }

    public List<EntityDimension> EntityDimensions { get; set; } = new();
}

public class EntityDimension
{
    public int EntityId { get; set; }
    public Entity? Entity { get; set; }
    public int DimensionId { get; set; }
    public Dimension? Dimension { get; set; }
}
