
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Data;
using MineGuess.Api.Ingestion;

namespace MineGuess.Api;

[ApiController]
[Route("api/v1/ingest")]
public class IngestController : ControllerBase
{
    [HttpPost("full")]
    public async Task<IActionResult> Full([FromServices] AppDb db, [FromQuery] string from = "1.0.0", [FromQuery] string to = "latest", CancellationToken ct = default)
    {
        var importer = new FullImporter();
        await importer.RunAsync(db, from, to, ct);
        return Ok(new { ok = true, from, to });
    }
}
