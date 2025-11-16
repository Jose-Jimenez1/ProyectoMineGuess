namespace MVP_ProyectoFinal.Models
{
    public class ResultadoIntentoVM
    {
        public string NombreBloque { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Funcion { get; set; } = string.Empty;
        public string Bioma { get; set; } = string.Empty;
        public string EsCrafteable { get; set; } = string.Empty;
        public string EsDeExterior { get; set; } = string.Empty;
        public int YearLanzamiento { get; set; }
        public int LongitudNombre { get; set; }
        public string CoincideInicial { get; set; } = string.Empty;
        public string ColorLongitudNombre { get; set; } = string.Empty;
        public string ColorCoincideInicial { get; set; } = string.Empty;
        public string HintLongitudNombre { get; set; } = string.Empty;
        public string ColorVersion { get; set; } = string.Empty;
        public string ColorBioma { get; set; } = string.Empty;
        public string ColorCrafteable { get; set; } = string.Empty;
        public string ColorExterior { get; set; } = string.Empty;
        public string ColorAnio { get; set; } = string.Empty;
        public string HintVersion { get; set; } = string.Empty;
        public string HintFuncion { get; set; } = string.Empty;
        public string HintAnio { get; set; } = string.Empty;
    }
}
