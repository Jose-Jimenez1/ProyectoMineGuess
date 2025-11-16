using System.Threading;
using System.Threading.Tasks;
using MineGuess.Api.Data;

namespace MineGuess.Api.Ingestion;

public class FullImporter
{
    public Task RunAsync(AppDb db, string from, string to, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
