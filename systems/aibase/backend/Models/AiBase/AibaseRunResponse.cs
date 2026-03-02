namespace Backend.Models.AiBase
{
    public class AibaseRunResponse
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string RunType { get; set; } = "dataset_build";
        public string Status { get; set; } = "queued";
        public string? EngineRunId { get; set; }
        public int ProgressPct { get; set; }
        public int RequestedByUserId { get; set; }
        public string TriggerSource { get; set; } = "manual";
        public string? InputJson { get; set; }
        public string? OutputJson { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
