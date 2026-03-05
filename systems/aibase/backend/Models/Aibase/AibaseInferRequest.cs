namespace Backend.Models.Aibase
{
    public class AibaseInferRequest
    {
        public string Input { get; set; } = "";
        public string? ContextJson { get; set; }
    }
}
