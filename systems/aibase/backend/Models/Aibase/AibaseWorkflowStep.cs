namespace Backend.Models.Aibase
{
    public class AibaseWorkflowStep
    {
        public int Order { get; set; }
        public string RunType { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Enabled { get; set; }
        public bool Required { get; set; }
        public bool Available { get; set; }
        public string Status { get; set; } = "pending";
        public int RunsCount { get; set; }
        public int CompletedCount { get; set; }
        public int ErrorCount { get; set; }
        public int? LastRunId { get; set; }
        public DateTime? LastRunAt { get; set; }
        public string? LastError { get; set; }
    }
}
