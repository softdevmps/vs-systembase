namespace Backend.Models.Entidades
{
    public class Incidenteaudio
    {
        public int Id { get; set; }
        public int? Incidenteid { get; set; }
        public string? Filepath { get; set; }
        public string? Format { get; set; }
        public decimal? Durationsec { get; set; }
        public string? Hash { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
