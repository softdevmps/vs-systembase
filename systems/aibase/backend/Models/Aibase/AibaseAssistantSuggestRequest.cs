namespace Backend.Models.Aibase
{
    public class AibaseAssistantSuggestRequest
    {
        public string Prompt { get; set; } = "";
        public string? Stage { get; set; }
        public int? ProjectId { get; set; }
    }
}
