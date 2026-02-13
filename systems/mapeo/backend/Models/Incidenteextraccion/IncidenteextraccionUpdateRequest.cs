namespace Backend.Models.Incidenteextraccion
{
    public class IncidenteextraccionUpdateRequest
    {
        public int? Incidenteid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string? Rawtext { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string? Jsonextract { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(2000)]
        public string? Scoresjson { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Modelversion { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(10)]
        public string? Language { get; set; }
        public decimal? Confidence { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
