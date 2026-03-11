namespace Backend.Models.Aibase
{
    public class AibaseDeployAssetsResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string? Endpoint { get; set; }
        public string? Health { get; set; }
        public string StackName { get; set; } = "";
        public string? ServiceName { get; set; }
        public string? ImageTag { get; set; }
        public string? BundleDir { get; set; }
        public string? ComposeFile { get; set; }
        public string? EnvFile { get; set; }
        public string? EngineEndpoint { get; set; }
        public string? EngineNotice { get; set; }
        public string DockerCommand { get; set; } = "";
        public string CurlSnippet { get; set; } = "";
        public string JavaScriptSnippet { get; set; } = "";
        public string PythonSnippet { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
    }
}
