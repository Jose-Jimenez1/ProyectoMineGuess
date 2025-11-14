using System;
using System.IO;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Data;
using MineGuess.Api.Models;
using MineGuess.Api.Ingest;
using MineGuess.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCaching();

builder.Services.AddDbContextPool<AppDb>(opt =>
{
    var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "mineguess.db");
    opt.UseSqlite($"Data Source={dbPath}");
    opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
var app = builder.Build();

// Optional auto-ingest on startup (set env var MINEGUESS_AUTO_INGEST=1)
if (Environment.GetEnvironmentVariable("MINEGUESS_AUTO_INGEST") == "1")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    var ing = new MineGuess.Api.Ingest.WikiIngestor(db);
    await ing.RunAsync("1.0");
}


app.UseSwagger();
app.UseSwaggerUI();
app.UseResponseCaching();

// Ensure DB & seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "App_Data"));
    db.Database.EnsureCreated();
    await SeedUtil.SeedAsync(db, env);
// Auto-ingest wiki once if DB is mostly empty
try
{
    var blocksCount = await db.Blocks.CountAsync();
    var entitiesCount = await db.Entities.CountAsync();
    if (blocksCount < 50 || entitiesCount < 20)
    {
        var ing = new WikiIngestor(db);
        await ing.RunAsync("1.0");
    }
}
catch { /* ignore network errors on startup */ }
}

app.MapGet("/", () => Results.Redirect("/swagger"));

// Versions
app.MapGet("/api/v1/versions", async (AppDb db) =>
{
    var list = await db.GameVersions.OrderBy(v => v.OrderKey).ToListAsync();
    return Results.Ok(list);
});

// Blocks list
app.MapGet("/api/v1/blocks", async (AppDb db, string? search, string? biome, string? dimension, string? max_version, int page = 1, int page_size = 20) =>
{
    var q = db.Blocks
        .Include(b => b.BlockBiomes).ThenInclude(bb => bb.Biome)
        .Include(b => b.Dimension)
        .Include(b => b.AddedInVersion)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(b => EF.Functions.Like(b.Name, $"%{search}%") || EF.Functions.Like(b.Key, $"%{search}%"));

    if (!string.IsNullOrWhiteSpace(biome))
        q = q.Where(b => b.BlockBiomes.Any(bb => bb.Biome!.Key == biome));

    if (!string.IsNullOrWhiteSpace(dimension))
        q = q.Where(b => b.Dimension!.Key == dimension);

    if (!string.IsNullOrWhiteSpace(max_version))
        q = q.Where(b => b.AddedInVersion != null && b.AddedInVersion!.Name.CompareTo(max_version) <= 0);

    var total = await q.CountAsync();
    var items = await q.OrderBy(b => b.Name)
                       .Skip((page - 1) * page_size)
                       .Take(page_size)
                       .Select(b => new {
                           b.Key, b.Name,
                           b.Category, b.IsBreakable,
                           Dimension = b.Dimension!.Key,
                           Biomes = b.BlockBiomes.Select(bb => bb.Biome!.Key),
                           AddedInVersion = b.AddedInVersion!.Name
                       }).ToListAsync();

    return Results.Ok(new { items, total });
});

app.MapGet("/api/v1/blocks/{key}", async (AppDb db, string key) =>
{
    var b = await db.Blocks
        .Include(x => x.BlockBiomes).ThenInclude(bb => bb.Biome)
        .Include(x => x.Dimension)
        .Include(x => x.AddedInVersion)
        .FirstOrDefaultAsync(x => x.Key == key);
    return b is null ? Results.NotFound() : Results.Ok(b);
});

// Entities
app.MapGet("/api/v1/entities", async (AppDb db, string? kind, string? dimension, string? max_version, int page = 1, int page_size = 20) =>
{
    var q = db.Entities
        .Include(e => e.EntityDimensions).ThenInclude(ed => ed.Dimension)
        .Include(e => e.AddedInVersion)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(kind))
        q = q.Where(e => e.Kind == kind);

    if (!string.IsNullOrWhiteSpace(dimension))
        q = q.Where(e => e.EntityDimensions.Any(d => d.Dimension!.Key == dimension));

    if (!string.IsNullOrWhiteSpace(max_version))
        q = q.Where(e => e.AddedInVersion != null && e.AddedInVersion!.Name.CompareTo(max_version) <= 0);

    var total = await q.CountAsync();
    var items = await q.OrderBy(e => e.Name)
                       .Skip((page - 1) * page_size)
                       .Take(page_size)
                       .Select(e => new {
                           e.Key, e.Name, e.Kind,
                           Dimensions = e.EntityDimensions.Select(d => d.Dimension!.Key),
                           AddedInVersion = e.AddedInVersion!.Name
                       }).ToListAsync();

    return Results.Ok(new { items, total });
});

app.MapGet("/api/v1/entities/{key}", async (AppDb db, string key) =>
{
    var e = await db.Entities
        .Include(x => x.EntityDimensions).ThenInclude(ed => ed.Dimension)
        .Include(x => x.AddedInVersion)
        .FirstOrDefaultAsync(x => x.Key == key);
    return e is null ? Results.NotFound() : Results.Ok(e);
});

// Suggest (autocomplete)
app.MapGet("/api/v1/suggest", async (AppDb db, string type, string q) =>
{
    q = q.Trim();
    if (string.IsNullOrWhiteSpace(q)) return Results.Ok(Array.Empty<string>());
    if (type == "block")
    {
        var s = await db.Blocks.Where(b => b.Name.Contains(q) || b.Key.Contains(q))
                               .OrderBy(b => b.Name)
                               .Take(10)
                               .Select(b => new { b.Key, b.Name })
                               .ToListAsync();
        return Results.Ok(s);
    }
    else if (type == "entity")
    {
        var s = await db.Entities.Where(e => e.Name.Contains(q) || e.Key.Contains(q))
                                 .OrderBy(e => e.Name)
                                 .Take(10)
                                 .Select(e => new { e.Key, e.Name })
                                 .ToListAsync();
        return Results.Ok(s);
    }
    return Results.BadRequest();
});

// Random
app.MapGet("/api/v1/random", async (AppDb db, string type, string? max_version) =>
{
    if (type == "block")
    {
        var q = db.Blocks.Include(b => b.AddedInVersion).AsQueryable();
        if (!string.IsNullOrWhiteSpace(max_version))
            q = q.Where(b => b.AddedInVersion != null && b.AddedInVersion!.Name.CompareTo(max_version) <= 0);
        var item = await q.OrderBy(x => EF.Functions.Random()).Select(b => b.Key).FirstOrDefaultAsync();
        return item is null ? Results.NotFound() : Results.Ok(new { key = item });
    }
    if (type == "entity")
    {
        var q = db.Entities.Include(e => e.AddedInVersion).AsQueryable();
        if (!string.IsNullOrWhiteSpace(max_version))
            q = q.Where(e => e.AddedInVersion != null && e.AddedInVersion!.Name.CompareTo(max_version) <= 0);
        var item = await q.OrderBy(x => EF.Functions.Random()).Select(e => e.Key).FirstOrDefaultAsync();
        return item is null ? Results.NotFound() : Results.Ok(new { key = item });
    }
    return Results.BadRequest();
});

// Compare
app.MapPost("/api/v1/compare", async (AppDb db, CompareReq req) =>
{
    if (req.Type == "block")
    {
        var guess = await db.Blocks.Include(b => b.AddedInVersion).Include(b => b.Dimension).FirstOrDefaultAsync(b => b.Key == req.GuessKey);
        var secret = await db.Blocks.Include(b => b.AddedInVersion).Include(b => b.Dimension).FirstOrDefaultAsync(b => b.Key == req.SecretKey);
        if (guess is null || secret is null) return Results.NotFound();

        var versionCmp = (guess.AddedInVersion?.Name ?? "").CompareTo(secret.AddedInVersion?.Name ?? "");
        string versionHint = versionCmp == 0 ? "=" : (versionCmp < 0 ? "↑" : "↓");
        string versionColor = versionCmp == 0 ? "verde" : "amarillo";

        var res = new {
            fields = new {
                version = new { value = guess.AddedInVersion?.Name, color = versionColor, hint = versionHint },
                dimension = new { value = guess.Dimension?.Key, color = (guess.DimensionId == secret.DimensionId ? "verde" : "rojo") },
                breakable = new { value = guess.IsBreakable, color = (guess.IsBreakable == secret.IsBreakable ? "verde" : "rojo") }
            }
        };
        return Results.Ok(res);
    }
    else if (req.Type == "entity")
    {
        var guess = await db.Entities.Include(e => e.AddedInVersion).FirstOrDefaultAsync(e => e.Key == req.GuessKey);
        var secret = await db.Entities.Include(e => e.AddedInVersion).FirstOrDefaultAsync(e => e.Key == req.SecretKey);
        if (guess is null || secret is null) return Results.NotFound();
        var versionCmp = (guess.AddedInVersion?.Name ?? "").CompareTo(secret.AddedInVersion?.Name ?? "");
        string versionHint = versionCmp == 0 ? "=" : (versionCmp < 0 ? "↑" : "↓");
        string versionColor = versionCmp == 0 ? "verde" : "amarillo";
        var res = new {
            fields = new {
                version = new { value = guess.AddedInVersion?.Name, color = versionColor, hint = versionHint },
                kind = new { value = guess.Kind, color = (guess.Kind == secret.Kind ? "verde" : "rojo") }
            }
        };
        return Results.Ok(res);
    }
    return Results.BadRequest();
});



// Ingest from wiki (best-effort). No auth for demo; protect in production.
app.MapPost("/api/v1/ingest/wiki", async (AppDb db) =>
{
    var ing = new MineGuess.Api.Ingest.WikiIngestor(db);
    await ing.RunAsync("1.0");
    return Results.Ok(new { status = "ok" });
});

app.Run();
