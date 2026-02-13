namespace Backend.Models.Incidentejobs
{
    public class IncidentejobsUpdateRequest
    {
        public int? Incidenteid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Status { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Step { get; set; }
        public int? Attempts { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string? Lasterror { get; set; }
        public DateTime? Createdat { get; set; }
        public DateTime? Updateat { get; set; }
    }
}
