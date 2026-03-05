namespace Backend.Models.Aibase
{
    public class AibaseOverviewResponse
    {
        public int TemplatesCount { get; set; }
        public int ProjectsCount { get; set; }
        public int RunsCount { get; set; }
        public int RunningCount { get; set; }
        public DateTime? LastRunAt { get; set; }
        public List<AibaseRunListItem> LastRuns { get; set; } = new();
    }

    public class AibaseRunListItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string RunType { get; set; } = "";
        public string Status { get; set; } = "";
        public int ProgressPct { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
