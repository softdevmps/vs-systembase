namespace Backend.Models.Aibase
{
    public class AibaseBootstrapRequest
    {
        public string? Prompt { get; set; }
        public string? ModelType { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectSlug { get; set; }
        public string? Language { get; set; }
        public string? Tone { get; set; }
        public bool CreateTemplate { get; set; } = true;
        public bool CreateProject { get; set; } = true;
    }
}
