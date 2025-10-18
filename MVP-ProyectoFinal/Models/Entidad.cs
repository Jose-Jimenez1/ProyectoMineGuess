namespace MVP_ProyectoFinal.Models.Elementos
{
    public class Entidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; 
        public int Vida { get; set; }
        public int Ataque { get; set; } 
        public string Dimension { get; set; } = string.Empty; 
        public int YearLanzamiento { get; set; }
    }
}