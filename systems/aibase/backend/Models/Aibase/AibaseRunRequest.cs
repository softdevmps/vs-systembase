namespace Backend.Models.Aibase
{
    public class AibaseRunRequest
    {
        public string RunType { get; set; } = "";
        public string? InputJson { get; set; }
    }
}
