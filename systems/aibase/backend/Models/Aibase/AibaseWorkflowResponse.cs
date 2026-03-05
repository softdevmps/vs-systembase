namespace Backend.Models.Aibase
{
    public class AibaseWorkflowResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string ProjectStatus { get; set; } = "draft";
        public string TemplateKey { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? NextRunType { get; set; }
        public bool CanInfer { get; set; }
        public DateTime? LastRunAt { get; set; }
        public List<AibaseWorkflowStep> Steps { get; set; } = new();
    }
}
