namespace Backend.Models.Aibase
{
    public class AibaseRunAllResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool Completed { get; set; }
        public string? LastError { get; set; }
        public List<AibaseRunAllStepResult> Steps { get; set; } = new();
    }

    public class AibaseRunAllStepResult
    {
        public string RunType { get; set; } = "";
        public string Label { get; set; } = "";
        public string Status { get; set; } = "";
        public bool Executed { get; set; }
        public bool Success { get; set; }
        public int? RunId { get; set; }
        public int ProgressPct { get; set; }
        public string? Error { get; set; }
        public string? OutputJson { get; set; }
        public DateTime At { get; set; }
    }
}
