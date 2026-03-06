namespace Backend.Models.Aibase
{
    public class AibaseAssistantSuggestResponse
    {
        public string Message { get; set; } = "";
        public string DetectedModelType { get; set; } = "general";
        public string? RecommendedStage { get; set; }
        public List<AibaseAssistantStageSuggestion> Suggestions { get; set; } = new();
    }

    public class AibaseAssistantStageSuggestion
    {
        public string Stage { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, object?> Fields { get; set; } = new();
    }
}
