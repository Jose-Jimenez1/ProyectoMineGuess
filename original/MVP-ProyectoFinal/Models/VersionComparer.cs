namespace MVP_ProyectoFinal.Models
{
    public static class VersionComparer
    {
        private static readonly Dictionary<string, int> _valoresVersiones = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Alpha 1.0", 100 },
            { "Alpha 1.0.1", 101 },
            { "Alpha 1.0.2", 102 },
            { "Alpha 1.0.11", 111 },
            { "Alpha 1.2", 120 },
            { "Alpha 1.2.0", 120 },
            { "Beta 1.2", 220 },
            { "Beta 1.8", 280 },
            { "1.0", 10000 },
            { "1.3", 10300 },
            { "1.3.1", 10310 },
            { "1.4", 10400 },
            { "1.5", 10500 },
            { "1.8", 10800 },
            { "1.11", 11100 },
            { "1.12", 11200 },
            { "1.13", 11300 },
            { "1.14", 11400 },
            { "1.15", 11500 },
            { "1.16", 11600 },
            { "1.17", 11700 },
            { "1.19", 11900 },
            { "1.20", 12000 }
        };

        public static int ObtenerValor(string version)
        {
            _valoresVersiones.TryGetValue(version, out int valor);
            return valor;
        }
    }
}