namespace Backend.Models.Incidentes
{
    public class IncidentesResponse
    {
        public int Id { get; set; }
        public DateTime Fechahora { get; set; }
        public string Lugartexto { get; set; }
        public string? Lugarnormalizado { get; set; }
        public int? Tipohechoid { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal? Confidence { get; set; }
        public string? Estado { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
