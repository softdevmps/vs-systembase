namespace Backend.Models.Entidades
{
    public class Incidenteubicacion
    {
        public int Id { get; set; }
        public int? Incidenteid { get; set; }
        public string? Fuente { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public string? Precision { get; set; }
        public string? Addressnormalized { get; set; }
    }
}
