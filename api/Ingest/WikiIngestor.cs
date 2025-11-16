using System.Threading.Tasks;
using MineGuess.Api.Data;

namespace MineGuess.Api.Ingest;

public class WikiIngestor
{
    private readonly AppDb _db;

    public WikiIngestor(AppDb db)
    {
        _db = db;
    }

    public Task RunAsync(string minVersion)
    {
        return Task.CompletedTask;
    }
}
