using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
        v = v.Trim();
        if (v.StartsWith("Alpha 1.0", StringComparison.OrdinalIgnoreCase)) return 2009;
        if (v.StartsWith("Alpha 1.1", StringComparison.OrdinalIgnoreCase) || v.StartsWith("Alpha 1.2", StringComparison.OrdinalIgnoreCase)) return 2010;
        if (v.StartsWith("Beta", StringComparison.OrdinalIgnoreCase)) return 2011;
        if (!v.StartsWith("1.", StringComparison.OrdinalIgnoreCase)) return 0;
        var parts = v.Split('.');
        if (parts.Length < 2) return 0;
        if (!int.TryParse(parts[1], out var minor)) return 0;
        switch (minor)
        {
            case 0: return 2011;
            case 1: return 2012;
            case 2: return 2012;
            case 3: return 2012;
            case 4: return 2012;
            case 5: return 2013;
            case 6: return 2013;
            case 7: return 2013;
            case 8: return 2014;
            case 9: return 2016;
            case 10: return 2016;
            case 11: return 2016;
            case 12: return 2017;
            case 13: return 2018;
            case 14: return 2019;
            case 15: return 2019;
            case 16: return 2020;
            case 17: return 2021;
            case 18: return 2021;
            case 19: return 2022;
            case 20: return 2023;
            case 21: return 2024;
            default: return 0;
        }
    }

    private static bool IsCraftable(BlockDto b)
    {
        var key = (b.Key ?? string.Empty).ToLowerInvariant();
        var category = (b.Category ?? string.Empty).ToLowerInvariant();

        if (category == "ore" || key.Contains("ore"))
            return false;

        if (category == "decorative" || category == "functional" || category == "light" || category == "redstone" || category == "storage")
            return true;

        if (string.IsNullOrEmpty(category))
        {
            if (key == "glass")
                return true;
        }

        return false;
    }

    private static bool IsExterior(BlockDto b)
    {
        var dimension = b.Dimension;
        if (!string.Equals(dimension, "overworld", StringComparison.OrdinalIgnoreCase))
            return false;

        var key = (b.Key ?? string.Empty).ToLowerInvariant();
        var category = (b.Category ?? string.Empty).ToLowerInvariant();

        if (category == "ore" || key.Contains("ore") || key.Contains("deepslate") || key.Contains("ancient_debris"))
            return false;

        return true;
    }


    public async Task<List<Bloque>> GetBlocksAsync()
    {
        var url = $"{_base}/api/v1/blocks?page=1&page_size=500";
        var doc = await _http.GetFromJsonAsync<BlocksList>(url);
        var items = doc?.items ?? new List<BlockDto>();
        return items.Select(b => new Bloque
        {
            Nombre = b.Name ?? b.Key ?? string.Empty,
            Version = b.AddedInVersion ?? string.Empty,
            Funcion = b.Category ?? string.Empty,
            Bioma = (b.Biomes?.FirstOrDefault() ?? b.Dimension ?? string.Empty),
            EsCrafteable = IsCraftable(b),
            EsDeExterior = IsExterior(b),
            YearLanzamiento = YearFromVersion(b.AddedInVersion)
        }).ToList();
    }

    public async Task<List<Entidad>> GetEntitiesAsync()
    {
        var url = $"{_base}/api/v1/entities?page=1&page_size=500";
        var doc = await _http.GetFromJsonAsync<EntitiesList>(url);
        var items = doc?.items ?? new List<EntityDto>();
        return items.Select(e => new Entidad
        {
            Id = 0,
            Nombre = e.Name ?? e.Key ?? string.Empty,
            Tipo = e.Kind ?? string.Empty,
            Vida = e.Health ?? 0,
            Ataque = e.Attack ?? 0,
            Dimension = e.Dimensions?.FirstOrDefault() ?? string.Empty,
            YearLanzamiento = YearFromVersion(e.AddedInVersion)
        }).ToList();
    }

    private record BlocksList(List<BlockDto> items, int total);
    private record BlockDto(string? Key, string? Name, string? Category, bool? IsBreakable, string? Dimension, List<string>? Biomes, string? AddedInVersion);
    private record EntitiesList(List<EntityDto> items, int total);
    private record EntityDto(string? Key, string? Name, string? Kind, int? Health, int? Attack, List<string>? Dimensions, string? AddedInVersion);
}