
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Data;
using MineGuess.Api.Models;

namespace MineGuess.Api.Ingest;

public class WikiIngestor
{
    private readonly HttpClient _http = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
    private readonly AppDb _db;

    public WikiIngestor(AppDb db)
    {
        _db = db;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MineGuess/1.0 (github.com/esteban-proyecto)"); // polite UA
    }

    public async Task RunAsync(string minVersion = "1.0", string? maxVersion = null)
    {
        await _db.Database.EnsureCreatedAsync();
        await EnsureVersionsAsync();
        await IngestBlocksAsync();
        await IngestEntitiesAsync();
        await _db.SaveChangesAsync();
    }

    private async Task EnsureVersionsAsync()
    {
        var url = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
        using var s = await _http.GetStreamAsync(url);
        var doc = await JsonDocument.ParseAsync(s);
        var versions = doc.RootElement.GetProperty("versions").EnumerateArray()
            .Where(v => v.GetProperty("type").GetString() == "release")
            .Select(v => new {
                id = v.GetProperty("id").GetString() ?? "",
                releaseTime = v.TryGetProperty("releaseTime", out var rt) ? (DateTime?)rt.GetDateTime() : null
            })
            .OrderBy(v => v.releaseTime ?? DateTime.MinValue)
            .ToList();

        int order = 10000;
        foreach (var v in versions)
        {
            if (string.Compare(v.id, "1.0") < 0) continue; // skip pre-1.0
            var existing = await _db.GameVersions.FirstOrDefaultAsync(x => x.Name == v.id);
            if (existing == null)
            {
                _db.GameVersions.Add(new GameVersion {
                    Name = v.id,
                    Channel = "release",
                    OrderKey = order++,
                    ReleaseDate = v.releaseTime.HasValue ? DateOnly.FromDateTime(v.releaseTime.Value) : null
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    // ---- Fandom wiki (legacy) fallback because minecraft.wiki blocks bots via robots.txt for scraping. ----
    // We'll use MediaWiki API on minecraft.fandom.com to list category members and parse infoboxes.

    private async Task IngestBlocksAsync()
    {
        // Category:Blocks
        var titles = await GetCategoryMembersAsync("Blocks");
        foreach (var title in titles)
        {
            var html = await GetParsedHtmlAsync(title);
            if (string.IsNullOrWhiteSpace(html)) continue;
            var info = ParseBlockInfobox(html);
            if (info == null) continue;

            // upsert
            var version = await _db.GameVersions.OrderBy(v => v.OrderKey).FirstOrDefaultAsync(v => v.Name == info.AddedIn) 
                          ?? await _db.GameVersions.OrderBy(v => v.OrderKey).FirstAsync();
            var dim = await GetOrAddDimensionAsync(info.Dimension ?? "overworld");

            var block = await _db.Blocks.FirstOrDefaultAsync(b => b.Key == info.Key) ?? new Block { Key = info.Key };
            block.Name = info.Name ?? info.Key;
            block.Category = info.Category;
            block.Hardness = info.Hardness;
            block.BlastResistance = info.BlastResistance;
            block.HasGravity = info.HasGravity;
            block.LightLevel = info.LightLevel;
            block.IsBreakable = info.IsBreakable ?? true;
            block.BestTool = info.BestTool;
            block.AddedInVersionId = version.Id;
            block.DimensionId = dim.Id;

            if (block.Id == 0) _db.Blocks.Add(block);

            // biomes (best effort: single biome if present)
            if (info.Biomes.Any())
            {
                foreach (var biomeKey in info.Biomes)
                {
                    var biome = await GetOrAddBiomeAsync(biomeKey);
                    var has = await _db.BlockBiomes.AnyAsync(bb => bb.BlockId == block.Id && bb.BiomeId == biome.Id);
                    if (!has) _db.BlockBiomes.Add(new BlockBiome { Block = block, Biome = biome });
                }
            }
        }
    }

    private async Task IngestEntitiesAsync()
    {
        // Category:Mobs
        var titles = await GetCategoryMembersAsync("Mobs");
        foreach (var title in titles)
        {
            var html = await GetParsedHtmlAsync(title);
            if (string.IsNullOrWhiteSpace(html)) continue;
            var info = ParseEntityInfobox(html);
            if (info == null) continue;

            var version = await _db.GameVersions.OrderBy(v => v.OrderKey).FirstOrDefaultAsync(v => v.Name == info.AddedIn) 
                          ?? await _db.GameVersions.OrderBy(v => v.OrderKey).FirstAsync();

            var ent = await _db.Entities.FirstOrDefaultAsync(e => e.Key == info.Key) ?? new Entity { Key = info.Key };
            ent.Name = info.Name ?? info.Key;
            ent.Kind = info.Kind;
            ent.Health = info.Health;
            ent.Attack = info.Attack;
            ent.AddedInVersionId = version.Id;

            if (ent.Id == 0) _db.Entities.Add(ent);

            foreach (var dkey in info.Dimensions)
            {
                var dim = await GetOrAddDimensionAsync(dkey);
                var has = await _db.EntityDimensions.AnyAsync(ed => ed.EntityId == ent.Id && ed.DimensionId == dim.Id);
                if (!has) _db.EntityDimensions.Add(new EntityDimension { Entity = ent, Dimension = dim });
            }
        }
    }

    // ---- Helpers ----

    private async Task<Dimension> GetOrAddDimensionAsync(string key)
    {
        key = key.ToLowerInvariant();
        var d = await _db.Dimensions.FirstOrDefaultAsync(x => x.Key == key);
        if (d != null) return d;
        d = new Dimension { Key = key, Name = CultureInfoCap(key) };
        _db.Dimensions.Add(d);
        await _db.SaveChangesAsync();
        return d;
    }

    private async Task<Biome> GetOrAddBiomeAsync(string key)
    {
        key = key.ToLowerInvariant().Replace(' ', '_');
        var b = await _db.Biomes.FirstOrDefaultAsync(x => x.Key == key);
        if (b != null) return b;
        b = new Biome { Key = key, Name = CultureInfoCap(key.Replace('_',' ')) };
        _db.Biomes.Add(b);
        await _db.SaveChangesAsync();
        return b;
    }

    private static string CultureInfoCap(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return char.ToUpperInvariant(s[0]) + (s.Length>1 ? s.Substring(1) : "");
    }

    private async Task<List<string>> GetCategoryMembersAsync(string category)
    {
        var result = new List<string>();
        string? cont = null;
        do
        {
            var url = $"https://minecraft.fandom.com/api.php?action=query&list=categorymembers&cmtitle=Category:{Uri.EscapeDataString(category)}&cmlimit=500&format=json" + (cont!=null?$"&cmcontinue={Uri.EscapeDataString(cont)}":"");
            var doc = await _http.GetFromJsonAsync<JsonDocument>(url);
            if (doc is null) break;
            var arr = doc.RootElement.GetProperty("query").GetProperty("categorymembers").EnumerateArray();
            foreach (var it in arr)
            {
                var ns = it.GetProperty("ns").GetInt32();
                var title = it.GetProperty("title").GetString();
                if (ns == 0 && !string.IsNullOrWhiteSpace(title))
                    result.Add(title!);
            }
            cont = doc.RootElement.TryGetProperty("continue", out var contEl) && contEl.TryGetProperty("cmcontinue", out var cme) ? cme.GetString() : null;
        } while (cont != null);
        return result;
    }

    private async Task<string?> GetParsedHtmlAsync(string title)
    {
        var url = $"https://minecraft.fandom.com/api.php?action=parse&page={Uri.EscapeDataString(title)}&prop=text&format=json";
        try
        {
            var doc = await _http.GetFromJsonAsync<JsonDocument>(url);
            if (doc is null) return null;
            var html = doc.RootElement.GetProperty("parse").GetProperty("text").GetProperty("*").GetString();
            return html;
        }
        catch { return null; }
    }

    // naive infobox parsing (best effort)
    private static readonly Regex RxBlockName = new(@"data-source=""name"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxInfoboxBlock = new(@"infobox.*?block", RegexOptions.IgnoreCase);
    private static readonly Regex RxHardness = new(@"data-source=""hardness"".*?>(?<v>[-\d\.]+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxResistance = new(@"data-source=""blast_resistance"".*?>(?<v>[-\d\.]+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxGravity = new(@"data-source=""gravity"".*?>(?<v>Yes|No)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxLight = new(@"data-source=""luminance"".*?>(?<v>\d+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxTool = new(@"data-source=""tool"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxAdded = new(@"data-source=""first_appeared"".*?>(?<v>[0-9\.]+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxDim = new(@"data-source=""dimension"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);

    private BlockInfo? ParseBlockInfobox(string html)
    {
        if (!RxInfoboxBlock.IsMatch(html)) return null;
        string name = MatchOr(html, RxBlockName);
        string key = Slug(name);
        decimal? hard = TryDec(MatchOr(html, RxHardness));
        decimal? br = TryDec(MatchOr(html, RxResistance));
        bool? grav = TryBool(MatchOr(html, RxGravity));
        int? light = TryInt(MatchOr(html, RxLight));
        string? tool = Clean(MatchOr(html, RxTool));
        string? added = Clean(MatchOr(html, RxAdded));
        string? dim = Clean(MatchOr(html, RxDim))?.ToLowerInvariant();
        return new BlockInfo { Key = key, Name = name, Hardness = hard, BlastResistance = br, HasGravity = grav, LightLevel = light, BestTool = tool, AddedIn = added, Dimension = MapDimension(dim) };
    }

    private static readonly Regex RxInfoboxMob = new(@"infobox.*?(mob|entity)", RegexOptions.IgnoreCase);
    private static readonly Regex RxMobName = new(@"data-source=""name"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxMobType = new(@"data-source=""type"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxMobHealth = new(@"data-source=""health"".*?>(?<v>\d+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxMobAttack = new(@"data-source=""attack"".*?>(?<v>\d+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxMobDim = new(@"data-source=""dimension"".*?>(?<v>[^<]+)<", RegexOptions.IgnoreCase|RegexOptions.Singleline);
    private static readonly Regex RxMobAdded = new(@"data-source=""first_appeared"".*?>(?<v>[0-9\.]+)", RegexOptions.IgnoreCase|RegexOptions.Singleline);

    private EntityInfo? ParseEntityInfobox(string html)
    {
        if (!RxInfoboxMob.IsMatch(html)) return null;
        string name = MatchOr(html, RxMobName);
        string key = Slug(name);
        string? kind = Clean(MatchOr(html, RxMobType))?.ToLowerInvariant();
        int? hp = TryInt(MatchOr(html, RxMobHealth));
        int? atk = TryInt(MatchOr(html, RxMobAttack));
        string? dim = Clean(MatchOr(html, RxMobDim))?.ToLowerInvariant();
        string? added = Clean(MatchOr(html, RxMobAdded));
        var dims = new List<string>();
        if (!string.IsNullOrWhiteSpace(dim))
            dims.Add(MapDimension(dim) ?? "overworld");
        return new EntityInfo { Key = key, Name = name, Kind = kind, Health = hp, Attack = atk, Dimensions = dims, AddedIn = added };
    }

    private static string? MatchOr(string html, Regex rx)
        => rx.Match(html) is var m && m.Success ? m.Groups["v"].Value : null;

    private static string Slug(string s) => s.Trim().ToLowerInvariant().Replace(' ', '_');
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? s : Regex.Replace(s, "<.*?>", "").Trim();
    private static decimal? TryDec(string? s) => decimal.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    private static int? TryInt(string? s) => int.TryParse(s, out var v) ? v : (int?)null;
    private static bool? TryBool(string? s) => s?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true ? true :
                                               s?.StartsWith("n", StringComparison.OrdinalIgnoreCase) == true ? false : null;

    private static string? MapDimension(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.ToLowerInvariant();
        if (s.Contains("overworld")) return "overworld";
        if (s.Contains("nether")) return "nether";
        if (s.Contains("the end") || s.Contains("end")) return "end";
        return null;
    }

    private record BlockInfo
    {
        public string Key { get; set; } = "";
        public string? Name { get; set; }
        public string? Category { get; set; }
        public decimal? Hardness { get; set; }
        public decimal? BlastResistance { get; set; }
        public bool? HasGravity { get; set; }
        public int? LightLevel { get; set; }
        public bool? IsBreakable { get; set; } = true;
        public string? BestTool { get; set; }
        public string? Dimension { get; set; }
        public List<string> Biomes { get; set; } = new();
        public string? AddedIn { get; set; }
    }

    private record EntityInfo
    {
        public string Key { get; set; } = "";
        public string? Name { get; set; }
        public string? Kind { get; set; }
        public int? Health { get; set; }
        public int? Attack { get; set; }
        public List<string> Dimensions { get; set; } = new();
        public string? AddedIn { get; set; }
    }
}
