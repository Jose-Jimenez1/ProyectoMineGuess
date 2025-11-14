using System;
using System.Net.Http.Json;
using MVP_ProyectoFinal.Models.Elementos;

namespace MVP_ProyectoFinal.Services;

public class ApiClientOriginal
{
    private readonly HttpClient _http;
    private readonly string _base;

    public ApiClientOriginal(string baseUrl)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        _base = baseUrl.TrimEnd('/');
    }

    private static int YearFromVersion(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return 0;
        if (v.StartsWith("Alpha 1.0", StringComparison.OrdinalIgnoreCase)) return 2009;
        if (v.StartsWith("Alpha 1.2", StringComparison.OrdinalIgnoreCase)) return 2010;
        if (v.StartsWith("Beta", StringComparison.OrdinalIgnoreCase)) return 2011;
        if (v.StartsWith("1.3.1")) return 2012;
        if (v.StartsWith("1.21")) return 2024;
        if (v.StartsWith("1.20")) return 2023;
        if (v.StartsWith("1.17")) return 2021;
        if (v.StartsWith("1.16")) return 2020;
        return 0;
    }

    public async Task<List<Bloque>> GetBlocksAsync()
    {
        var url = $"{_base}/api/v1/blocks?page=1&page_size=500";
        var doc = await _http.GetFromJsonAsync<BlocksList>(url);
        var items = doc?.items ?? new List<BlockDto>();
        return items.Select(b => new Bloque {
            Nombre = b.Name ?? b.Key ?? "",
            Version = b.Category ?? b.AddedInVersion ?? "",
            Bioma = (b.Biomes?.FirstOrDefault() ?? b.Dimension ?? ""),
            EsDestructible = b.IsBreakable ?? true,
            EsDeExterior = (b.Dimension?.ToLowerInvariant() == "overworld"),
            YearLanzamiento = YearFromVersion(b.Category ?? b.AddedInVersion)
        }).ToList();
    }

    public async Task<List<Entidad>> GetEntitiesAsync()
    {
        var url = $"{_base}/api/v1/entities?page=1&page_size=500";
        var doc = await _http.GetFromJsonAsync<EntitiesList>(url);
        var items = doc?.items ?? new List<EntityDto>();
        return items.Select(e => new Entidad {
            Id = 0,
            Nombre = e.Name ?? e.Key ?? "",
            Tipo = e.Kind ?? "",
            Vida = e.Health ?? 0,
            Ataque = e.Attack ?? 0,
            Dimension = e.Dimensions?.FirstOrDefault() ?? "",
            YearLanzamiento = YearFromVersion(e.AddedInVersion)
        }).ToList();
    }

    private record BlocksList(List<BlockDto> items, int total);
    private record BlockDto(string? Key, string? Name, string? Category, bool? IsBreakable, string? Dimension, List<string>? Biomes, string? AddedInVersion);
    private record EntitiesList(List<EntityDto> items, int total);
    private record EntityDto(string? Key, string? Name, string? Kind, int? Health, int? Attack, List<string>? Dimensions, string? AddedInVersion);
}
