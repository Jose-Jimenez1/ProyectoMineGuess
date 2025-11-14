
namespace MineGuess.Api.Models;

public class GameVersion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Channel { get; set; } = "release"; // alpha|beta|snapshot|release
    public int OrderKey { get; set; } // monotonically increasing for easy compare
    public DateOnly? ReleaseDate { get; set; }

    // convenience: parse "1.21.3" to an order key externally
}
