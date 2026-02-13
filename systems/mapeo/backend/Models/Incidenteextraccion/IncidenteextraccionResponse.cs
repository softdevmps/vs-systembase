namespace Backend.Models.Incidenteextraccion
{
    public class IncidenteextraccionResponse
    {
        public int Id { get; set; }
        public int? Incidenteid { get; set; }
        public string? Rawtext { get; set; }
        public string? Jsonextract { get; set; }
        public string? Scoresjson { get; set; }
        public string? Modelversion { get; set; }
        public string? Language { get; set; }
        public decimal? Confidence { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
