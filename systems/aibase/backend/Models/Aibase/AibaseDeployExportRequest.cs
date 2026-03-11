namespace Backend.Models.Aibase
{
    public class AibaseDeployExportRequest
    {
        public string? ServiceName { get; set; }
        public string? ImageTag { get; set; }
        public int? HostPort { get; set; }
        public int? ContainerPort { get; set; }
        public string? Endpoint { get; set; }
        public string? HealthUrl { get; set; }
        public Dictionary<string, string>? ExtraEnv { get; set; }
    }
}
