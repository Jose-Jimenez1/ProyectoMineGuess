namespace MVP_ProyectoFinal.Models
{
    public class ResultadoIntentoEntidadVM
    {
        public string NombreEntidad { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Vida { get; set; }
        public int Ataque { get; set; }
        public string Dimension { get; set; } = string.Empty;
        public int YearLanzamiento { get; set; }
        public int LongitudNombre { get; set; }
        public string CoincideInicial { get; set; } = string.Empty;
        public string ColorLongitudNombre { get; set; } = string.Empty;
        public string ColorCoincideInicial { get; set; } = string.Empty;
        public string HintLongitudNombre { get; set; } = string.Empty;


        public string ColorTipo { get; set; } = string.Empty;
        public string ColorVida { get; set; } = string.Empty;
        public string ColorAtaque { get; set; } = string.Empty;
        public string ColorDimension { get; set; } = string.Empty;
        public string ColorAnio { get; set; } = string.Empty;

        public string HintVida { get; set; } = string.Empty;
        public string HintAtaque { get; set; } = string.Empty;
        public string HintAnio { get; set; } = string.Empty;
    }
}