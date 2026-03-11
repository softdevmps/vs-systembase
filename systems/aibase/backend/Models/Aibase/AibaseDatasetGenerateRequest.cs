namespace Backend.Models.Aibase
{
    public class AibaseDatasetGenerateRequest
    {
        public List<string>? Topics { get; set; }
        public string? DatasetName { get; set; }
        public int? MaxWikipediaResults { get; set; }
        public int? MaxExpandedQueries { get; set; }
        public int? ChunkSize { get; set; }
        public int? ChunkOverlap { get; set; }
        public double? SleepSeconds { get; set; }
        public bool? ResetTopicFolders { get; set; }
    }
}
