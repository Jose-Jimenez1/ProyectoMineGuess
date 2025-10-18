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
            { "1.3.1", 331 }
        };

        public static int ObtenerValor(string version)
        {
            _valoresVersiones.TryGetValue(version, out int valor);
            return valor;
        }
    }
}