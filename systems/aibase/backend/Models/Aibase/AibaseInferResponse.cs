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
        public DateTime CreatedAt { get; set; }
    }
}
