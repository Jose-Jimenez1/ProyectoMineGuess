namespace MVP_ProyectoFinal.Models
{
    public class ResultadoIntentoVM
    {
        public string NombreBloque { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Bioma { get; set; } = string.Empty;
        public string EsDestructible { get; set; } = string.Empty;
        public string EsDeExterior { get; set; } = string.Empty;
        public int YearLanzamiento { get; set; }
        public string ColorVersion { get; set; } = string.Empty;
        public string ColorBioma { get; set; } = string.Empty;
        public string ColorDestructible { get; set; } = string.Empty;
        public string ColorExterior { get; set; } = string.Empty;
        public string ColorAnio { get; set; } = string.Empty;
        public string HintVersion { get; set; } = string.Empty;
        public string HintAnio { get; set; } = string.Empty;

        public int LongitudNombre { get; set; }
        public string ColorLongitud { get; set; } = string.Empty;
        public string HintLongitud { get; set; } = string.Empty;
        public string Inicial { get; set; } = string.Empty;
        public string ColorInicial { get; set; } = string.Empty;
        public int NumeroPalabras { get; set; }
        public string ColorNumeroPalabras { get; set; } = string.Empty;
    }
}