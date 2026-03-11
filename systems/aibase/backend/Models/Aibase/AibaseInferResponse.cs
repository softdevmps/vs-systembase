namespace Backend.Models.Aibase
{
    public class AibaseInferResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string TemplateKey { get; set; } = "";
        public string Input { get; set; } = "";
        public string Output { get; set; } = "";
        public string? OutputJson { get; set; }
        public bool IsMock { get; set; }
        public string Provider { get; set; } = "";
        public string? Model { get; set; }
        public string? Endpoint { get; set; }
        public string? EngineNotice { get; set; }
        public bool UsedFallback { get; set; }
        public double? QualityScore { get; set; }
        public string? TraceId { get; set; }
        public string? DiagnosticsJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
