namespace Backend.Models.Aibase
{
    public class AibaseDatasetMergeRequest
    {
        public List<string>? SourcePaths { get; set; }
        public string? DatasetName { get; set; }
        public bool? Deduplicate { get; set; }
    }
}
