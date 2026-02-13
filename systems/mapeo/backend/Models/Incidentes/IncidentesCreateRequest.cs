namespace Backend.Models.Incidentes
{
    public class IncidentesCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Fechahora { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(255)]
        public string Lugartexto { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(255)]
        public string? Lugarnormalizado { get; set; }
        public int? Tipohechoid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string? Descripcion { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal? Confidence { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Estado { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
