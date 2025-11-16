namespace MVP_ProyectoFinal.Models.Elementos
{
    public class Bloque
    {
        public string Nombre { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Funcion { get; set; } = string.Empty;
        public string Bioma { get; set; } = string.Empty;
        public bool EsCrafteable { get; set; }
        public bool EsDeExterior { get; set; }
        public int YearLanzamiento { get; set; }
    }
}
