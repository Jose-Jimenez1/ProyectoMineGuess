
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using MineGuess.Api.Data;
using MineGuess.Api.Models;

namespace MineGuess.Api.Ingestion;

public class FullImporter
{
    private readonly HttpClient _http = new HttpClient();

    private static int ParseOrderKey(string v)
    {
        // convert "1.21.3" -> 12103, "1.8"->10800
        var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int major = parts.Length > 0 ? int.Parse(Regex.Match(parts[0], @"\d+").Value) : 0;
        int minor = parts.Length > 1 ? int.Parse(Regex.Match(parts[1], @"\d+").Value) : 0;
        int patch = parts.Length > 2 ? int.Parse(Regex.Match(parts[2], @"\d+").Value) : 0;
        return major*10000 + minor*100 + patch;
    }

    public async Task RunAsync(AppDb db, string fromVersion="1.0.0", string toVersion="latest", CancellationToken ct=default)
    {
        // 1) Get list of supported pc versions from minecraft-data
        // https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/common/versions.json
        var versionsUrl = "https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/common/versions.json";
        var versionsDoc = await _http.GetFromJsonAsync<VersionsDoc>(versionsUrl, cancellationToken: ct)
                        ?? new VersionsDoc(new());

        var fromKey = ParseOrderKey(fromVersion.Replace("release ",""));
        var toKey = int.MaxValue; // latest
        if (toVersion != "latest")
            toKey = ParseOrderKey(toVersion);

        var pcVersions = versionsDoc.versions
            .Where(v => v.release && v.type == "pc" && v.javaVersion is not null)
            .OrderBy(v => v.majorVersion)
            .ThenBy(v => v.minorVersion)
            .ThenBy(v => v.patchVersion)
            .ToList();

        // 2) Accumulate earliest version per block/entity
        var blockFirst = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entityFirst = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in pcVersions)
        {
            var verStr = v.minecraftVersion;
            if (string.IsNullOrWhiteSpace(verStr)) continue;
            var ok = ParseOrderKey(verStr);
            if (ok < fromKey || ok > toKey) continue;

            // blocks.json
            var blocksUrl = $"https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/{verStr}/blocks.json";
            try
            {
                var blocks = await _http.GetFromJsonAsync<List<MdBlock>>(blocksUrl, cancellationToken: ct);
                if (blocks != null)
                {
                    foreach (var b in blocks)
                    {
                        var key = b.name ?? b.displayName ?? "";
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        if (!blockFirst.ContainsKey(key)) blockFirst[key] = verStr;
                    }
                }
            }
            catch { /* ignore version gaps */ }

            // entities.json
            var entitiesUrl = $"https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/{verStr}/entities.json";
            try
            {
                var ents = await _http.GetFromJsonAsync<List<MdEntity>>(entitiesUrl, cancellationToken: ct);
                if (ents != null)
                {
                    foreach (var e in ents)
                    {
                        var key = e.name ?? e.displayName ?? "";
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        if (!entityFirst.ContainsKey(key)) entityFirst[key] = verStr;
                    }
                }
            }
            catch { /* ignore version gaps */ }
        }

        // 3) Reset DB
        await using var trx = await db.Database.BeginTransactionAsync(ct);
        db.BlockBiomes.RemoveRange(db.BlockBiomes);
        db.EntityDimensions.RemoveRange(db.EntityDimensions);
        db.Blocks.RemoveRange(db.Blocks);
        db.Entities.RemoveRange(db.Entities);
        db.GameVersions.RemoveRange(db.GameVersions);
        await db.SaveChangesAsync(ct);

        // Add base versions table from collected keys
        var versionNames = blockFirst.Values.Concat(entityFirst.Values).Distinct().ToList();
        versionNames.Sort((a,b) => ParseOrderKey(a).CompareTo(ParseOrderKey(b)));
        foreach (var vname in versionNames)
        {
            db.GameVersions.Add(new GameVersion {
                Name = vname, Channel = "release", OrderKey = ParseOrderKey(vname)
            });
        }
        // Ensure base dimensions
        var dimOver = new Dimension { Key="overworld", Name="Overworld" };
        var dimNether = new Dimension { Key="nether", Name="Nether" };
        var dimEnd = new Dimension { Key="end", Name="The End" };
        db.Dimensions.AddRange(dimOver, dimNether, dimEnd);
        await db.SaveChangesAsync(ct);

        // 4) Insert blocks
        foreach (var kv in blockFirst.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var name = kv.Key;
            var vname = kv.Value;
            var v = await db.GameVersions.FirstAsync(x => x.Name == vname, ct);
            var b = new Block {
                Key = ToKey(name),
                Name = name,
                Category = null,
                Hardness = null,
                BlastResistance = null,
                HasGravity = null,
                LightLevel = null,
                IsBreakable = true,
                BestTool = null,
                DimensionId = dimOver.Id,
                AddedInVersionId = v.Id
            };
            db.Blocks.Add(b);
        }
        await db.SaveChangesAsync(ct);

        // 5) Insert entities (enriched from wiki when possible)
        foreach (var kv in entityFirst.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var name = kv.Key;
            var vname = kv.Value;
            var v = await db.GameVersions.FirstAsync(x => x.Name == vname, ct);
            var (kind, health, attack, dims) = await EnrichEntityFromWikiAsync(name, ct);

            var e = new Entity {
                Key = ToKey(name),
                Name = name,
                Kind = kind,
                Health = health,
                Attack = attack,
                SpawnRulesJson = null,
                AddedInVersionId = v.Id
            };
            db.Entities.Add(e);
            await db.SaveChangesAsync(ct);

            foreach (var d in dims)
            {
                var dd = d switch {
                    "nether" => dimNether,
                    "end" => dimEnd,
                    _ => dimOver
                };
                db.EntityDimensions.Add(new EntityDimension { EntityId = e.Id, DimensionId = dd.Id });
            }
        }

        await db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
    }

    private static string ToKey(string name) =>
        Regex.Replace(name.ToLowerInvariant().Replace(' ', '_'), @"[^a-z0-9_]", "");

    private async Task<(string kind, int? health, int? attack, List<string> dimensions)>
        EnrichEntityFromWikiAsync(string entityName, CancellationToken ct)
    {
        // Default values
        string kind = "";
        int? health = null;
        int? attack = null;
        var dimensions = new List<string>();

        try
        {
            // English wiki page
            var url = $"https://minecraft.wiki/w/{Uri.EscapeDataString(entityName)}";
            var html = await _http.GetStringAsync(url, ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Try infobox rows
            var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'infobox')]//tr") ?? new HtmlNodeCollection(null);
            foreach (var r in rows)
            {
                var header = r.SelectSingleNode("./th")?.InnerText?.Trim().ToLowerInvariant() ?? "";
                var val = r.SelectSingleNode("./td")?.InnerText?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(val)) continue;
                if (header.Contains("type"))
                {
                    if (val.ToLower().Contains("hostile")) kind = "hostile";
                    else if (val.ToLower().Contains("neutral")) kind = "neutral";
                    else if (val.ToLower().Contains("passive")) kind = "pasivo";
                }
                if (header.Contains("health"))
                {
                    var m = Regex.Match(val.Replace(",", ""), @"(\d+)\s*(?:♥|hp|health)?", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var h)) health = h;
                }
                if (header.Contains("attack") || header.Contains("damage"))
                {
                    var m = Regex.Match(val.Replace(",", ""), @"(\d+)", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var a)) attack = a;
                }
            }

            // Heuristic for dimensions based on text occurrence
            var text = doc.DocumentNode.InnerText.ToLowerInvariant();
            if (text.Contains("nether")) dimensions.Add("nether");
            if (text.Contains("the end") || text.Contains("end island") || text.Contains("enderman")) dimensions.Add("end");
            if (!dimensions.Contains("nether") && !dimensions.Contains("end")) dimensions.Add("overworld");
        }
        catch
        {
            // ignore wiki failures
            dimensions.Add("overworld");
        }

        return (kind, health, attack, dimensions.Distinct().ToList());
    }

    private record VersionsDoc(List<VersionEntry> versions);
    private record VersionEntry(string? minecraftVersion, int majorVersion, int minorVersion, int patchVersion, bool release, string type, object? javaVersion);
    private record MdBlock(string? name, string? displayName);
    private record MdEntity(string? name, string? displayName);
}
