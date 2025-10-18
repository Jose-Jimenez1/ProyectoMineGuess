using MVP_ProyectoFinal.Models.Elementos;

namespace MVP_ProyectoFinal.Models
{
    public static class RepositorioBloques
    {
        private static readonly Random _random = new Random();

        private static readonly List<Bloque> _bloques = new List<Bloque>
        {
            new Bloque { Nombre = "Tierra", Version = "Alpha 1.0", Bioma = "Pradera", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Diamante", Version = "Alpha 1.2", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Bedrock", Version = "Alpha 1.0", Bioma = "Todos", EsDestructible = false, EsDeExterior = false, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Pasto", Version = "Alpha 1.0", Bioma = "Pradera", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Arena", Version = "Alpha 1.0", Bioma = "Desierto", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Obsidiana", Version = "Alpha 1.0.11", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Piedra", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Adoquín", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Madera de Roble", Version = "Alpha 1.0", Bioma = "Bosque", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Grava", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Carbón", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Hierro", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Oro", Version = "Alpha 1.0", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Lapis Lázuli", Version = "Beta 1.2", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2011 },
            new Bloque { Nombre = "Redstone", Version = "Alpha 1.0.1", Bioma = "Cueva", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Esmeralda", Version = "1.3.1", Bioma = "Montaña", EsDestructible = true, EsDeExterior = false, YearLanzamiento = 2012 },
            new Bloque { Nombre = "Mesa de Crafteo", Version = "Alpha 1.0.11", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Horno", Version = "Alpha 1.0.11", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Calabaza", Version = "Alpha 1.2.0", Bioma = "Pradera", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Lana Blanca", Version = "Alpha 1.0", Bioma = "Pradera", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Cristal", Version = "Alpha 1.0.2", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 },
            new Bloque { Nombre = "Ladrillos", Version = "Alpha 1.0.11", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Librería", Version = "Alpha 1.2.0", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2010 },
            new Bloque { Nombre = "Caldero", Version = "Beta 1.8", Bioma = "Crafteable", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2011 },
            new Bloque { Nombre = "Hojas de Roble", Version = "Alpha 1.0", Bioma = "Bosque", EsDestructible = true, EsDeExterior = true, YearLanzamiento = 2009 }
        };

        public static Bloque? ObtenerPorNombre(string nombre)
        {
            return _bloques.FirstOrDefault(b => b.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        }

        public static Bloque? ObtenerBloqueAleatorio()
        {
            if (_bloques.Count == 0) return null;
            int index = _random.Next(_bloques.Count);
            return _bloques[index];
        }

        public static List<Bloque> ObtenerTodos() => _bloques;
    }
}