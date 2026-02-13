namespace Backend.Models.Entidades
{
    public class Incidentejobs
    {
        public int Id { get; set; }
        public int? Incidenteid { get; set; }
        public string? Status { get; set; }
        public string? Step { get; set; }
        public int? Attempts { get; set; }
        public string? Lasterror { get; set; }
        public DateTime? Createdat { get; set; }
        public DateTime? Updateat { get; set; }
    }
}
