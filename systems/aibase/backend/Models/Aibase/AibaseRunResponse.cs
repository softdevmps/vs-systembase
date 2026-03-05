namespace Backend.Models.Aibase
{
    public class AibaseRunResponse
    {
        public int RunId { get; set; }
        public int ProjectId { get; set; }
        public string RunType { get; set; } = "";
        public string Status { get; set; } = "";
        public int ProgressPct { get; set; }
        public string? OutputJson { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsMock { get; set; }
    }
}
