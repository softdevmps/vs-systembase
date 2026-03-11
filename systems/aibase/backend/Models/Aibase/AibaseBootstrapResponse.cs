namespace Backend.Models.Aibase
{
    public class AibaseBootstrapResponse
    {
        public string Message { get; set; } = "";
        public string DetectedModelType { get; set; } = "general";
        public int? TemplateId { get; set; }
        public string? TemplateKey { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectSlug { get; set; }
        public string? ProjectName { get; set; }
        public bool TemplateCreated { get; set; }
        public bool ProjectCreated { get; set; }
        public bool TemplateAlreadyExists { get; set; }
        public bool ProjectAlreadyExists { get; set; }
    }
}
