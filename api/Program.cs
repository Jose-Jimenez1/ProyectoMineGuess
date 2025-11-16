using Microsoft.EntityFrameworkCore;
using MineGuess.Api;
using MineGuess.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCaching();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "mineguess.db");
    connectionString = $"Data Source={dbPath}";
}

builder.Services.AddDbContext<AppDb>(opt =>
{
    opt.UseSqlite(connectionString);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseResponseCaching();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    db.Database.EnsureCreated();
    await SeedUtil.SeedAsync(db, env);
}

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/v1/blocks", async (AppDb db, int page, int page_size, string? search, string? biome, string? dimension, string? max_version) =>
{
    if (page <= 0) page = 1;
    if (page_size <= 0 || page_size > 500) page_size = 500;

    var q = db.Blocks
        .Include(b => b.Dimension)
        .Include(b => b.AddedInVersion)
        .Include(b => b.BlockBiomes).ThenInclude(bb => bb.Biome)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(b => EF.Functions.Like(b.Name, $"%{search}%") || EF.Functions.Like(b.Key, $"%{search}%"));

    if (!string.IsNullOrWhiteSpace(biome))
        q = q.Where(b => b.BlockBiomes.Any(bb => bb.Biome != null && bb.Biome.Key == biome));

    if (!string.IsNullOrWhiteSpace(dimension))
        q = q.Where(b => b.Dimension != null && b.Dimension.Key == dimension);

    if (!string.IsNullOrWhiteSpace(max_version))
        q = q.Where(b => b.AddedInVersion != null && string.Compare(b.AddedInVersion.Name, max_version) <= 0);

    var total = await q.CountAsync();

    var items = await q
        .OrderBy(b => b.Name)
        .Skip((page - 1) * page_size)
        .Take(page_size)
        .Select(b => new
        {
            b.Key,
            b.Name,
            b.Category,
            b.IsBreakable,
            Dimension = b.Dimension != null ? b.Dimension.Key : null,
            Biomes = b.BlockBiomes.Select(bb => bb.Biome != null ? bb.Biome.Key : null).Where(x => x != null),
            AddedInVersion = b.AddedInVersion != null ? b.AddedInVersion.Name : null
        })
        .ToListAsync();

    return Results.Ok(new { items, total });
});

app.MapGet("/api/v1/entities", async (AppDb db, int page, int page_size, string? search, string? dimension, string? max_version) =>
{
    if (page <= 0) page = 1;
    if (page_size <= 0 || page_size > 500) page_size = 500;

    var q = db.Entities
        .Include(e => e.AddedInVersion)
        .Include(e => e.EntityDimensions).ThenInclude(ed => ed.Dimension)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(e => EF.Functions.Like(e.Name, $"%{search}%") || EF.Functions.Like(e.Key, $"%{search}%"));

    if (!string.IsNullOrWhiteSpace(dimension))
        q = q.Where(e => e.EntityDimensions.Any(ed => ed.Dimension != null && ed.Dimension.Key == dimension));

    if (!string.IsNullOrWhiteSpace(max_version))
        q = q.Where(e => e.AddedInVersion != null && string.Compare(e.AddedInVersion.Name, max_version) <= 0);

    var total = await q.CountAsync();

    var items = await q
        .OrderBy(e => e.Name)
        .Skip((page - 1) * page_size)
        .Take(page_size)
        .Select(e => new
        {
            e.Key,
            e.Name,
            e.Kind,
            e.Health,
            e.Attack,
            Dimensions = e.EntityDimensions.Select(ed => ed.Dimension != null ? ed.Dimension.Key : null).Where(x => x != null),
            AddedInVersion = e.AddedInVersion != null ? e.AddedInVersion.Name : null
        })
        .ToListAsync();

    return Results.Ok(new { items, total });
});

app.MapGet("/api/v1/suggest", async (AppDb db, string type, string q) =>
{
    q = q.Trim();
    if (string.IsNullOrWhiteSpace(q))
        return Results.Ok(Array.Empty<object>());

    if (type == "block")
    {
        var s = await db.Blocks
            .Where(b => b.Name.Contains(q) || b.Key.Contains(q))
            .OrderBy(b => b.Name)
            .Take(10)
            .Select(b => new { b.Key, b.Name })
            .ToListAsync();
        return Results.Ok(s);
    }
    else if (type == "entity")
    {
        var s = await db.Entities
            .Where(e => e.Name.Contains(q) || e.Key.Contains(q))
            .OrderBy(e => e.Name)
            .Take(10)
            .Select(e => new { e.Key, e.Name })
            .ToListAsync();
        return Results.Ok(s);
    }

    return Results.BadRequest();
});

app.Run();
