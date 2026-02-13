namespace Backend.Models.Incidenteaudio
{
    public class IncidenteaudioCreateRequest
    {
        public int? Incidenteid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Filepath { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Format { get; set; }
        public decimal? Durationsec { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(64)]
        public string? Hash { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
