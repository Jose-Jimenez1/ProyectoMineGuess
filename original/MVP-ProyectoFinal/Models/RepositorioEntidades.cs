using MVP_ProyectoFinal.Models.Elementos;

namespace MVP_ProyectoFinal.Models
{
    public static class RepositorioEntidades
    {
        private static bool _useApi = false;

        private static string? _apiBaseUrl = null;

        private static List<Entidad>? _cacheApi = null;

        public static void ConfigurarApi(bool useApi, string? baseUrl) { _useApi = useApi; _apiBaseUrl = baseUrl; _cacheApi = null; }

        private static readonly Random _random = new Random();

        private static readonly List<Entidad> _entidades = new List<Entidad>
        {
            new Entidad { Id = 1, Nombre = "Zombie", Tipo = "Hostil", Vida = 20, Ataque = 3, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 2, Nombre = "Creeper", Tipo = "Hostil", Vida = 20, Ataque = 49, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 3, Nombre = "Esqueleto", Tipo = "Hostil", Vida = 20, Ataque = 4, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 4, Nombre = "Vaca", Tipo = "Pasivo", Vida = 10, Ataque = 0, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 5, Nombre = "Cerdo", Tipo = "Pasivo", Vida = 10, Ataque = 0, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 6, Nombre = "Oveja", Tipo = "Pasivo", Vida = 8, Ataque = 0, Dimension = "Overworld", YearLanzamiento = 2009 },
            new Entidad { Id = 7, Nombre = "Enderman", Tipo = "Neutral", Vida = 40, Ataque = 7, Dimension = "The End", YearLanzamiento = 2011 },
            new Entidad { Id = 8, Nombre = "Blaze", Tipo = "Hostil", Vida = 20, Ataque = 6, Dimension = "Nether", YearLanzamiento = 2011 },
            new Entidad { Id = 9, Nombre = "Aldeano", Tipo = "Pasivo", Vida = 20, Ataque = 0, Dimension = "Overworld", YearLanzamiento = 2011 },
            new Entidad { Id = 10, Nombre = "Lobo", Tipo = "Neutral", Vida = 8, Ataque = 4, Dimension = "Overworld", YearLanzamiento = 2010 },
            new Entidad { Id = 11, Nombre = "Ghast", Tipo = "Hostil", Vida = 10, Ataque = 17, Dimension = "Nether", YearLanzamiento = 2010 },
            new Entidad { Id = 12, Nombre = "Murciélago", Tipo = "Pasivo", Vida = 6, Ataque = 0, Dimension = "Overworld", YearLanzamiento = 2012 },
            new Entidad { Id = 13, Nombre = "Wither", Tipo = "Hostil", Vida = 300, Ataque = 8, Dimension = "Crafteable", YearLanzamiento = 2012 },
            new Entidad { Id = 14, Nombre = "Shulker", Tipo = "Hostil", Vida = 30, Ataque = 4, Dimension = "The End", YearLanzamiento = 2015 },
            new Entidad { Id = 15, Nombre = "Ajolote", Tipo = "Pasivo", Vida = 14, Ataque = 2, Dimension = "Overworld", YearLanzamiento = 2021 }
        };

        public static List<Entidad> ObtenerTodos()
        {
            if (_useApi && !string.IsNullOrWhiteSpace(_apiBaseUrl))
            {
                try {
                    if (_cacheApi is null)
                    {
                        var cli = new Services.ApiClientOriginal(_apiBaseUrl!);
                        _cacheApi = cli.GetEntitiesAsync().GetAwaiter().GetResult();
                    }
                    if (_cacheApi is not null && _cacheApi.Count > 0) return _cacheApi;
                } catch {
                    _useApi = false;
                    _cacheApi = new List<Entidad>();
                }
            }
            return _entidades;
        }

        public static Entidad? ObtenerPorNombre(string nombre)
        {
            var lista = ObtenerTodos();
            return lista.FirstOrDefault(e => e.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        }

        public static Entidad? ObtenerEntidadAleatoria()
        {
            var lista = ObtenerTodos();
            if (lista.Count == 0) return null;
            int index = new Random().Next(lista.Count);
            return lista[index];
        }
    }
}