
namespace MineGuess.Api;

public record CompareReq(string Type, string GuessKey, string SecretKey);

public record BlockSeed(
    string key, string name, string category, decimal? hardness, decimal? blastResistance,
    bool? hasGravity, int? lightLevel, bool isBreakable, string? bestTool,
    string dimension, string addedIn, string[]? biomes);

public record EntitySeed(
    string key, string name, string kind, int? health, int? attack, string? spawnRules,
    string addedIn, string[]? dimensions);
