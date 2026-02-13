namespace Backend.Models.Incidenteubicacion
{
    public class IncidenteubicacionCreateRequest
    {
        public int? Incidenteid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Fuente { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Precision { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(255)]
        public string? Addressnormalized { get; set; }
    }
}
