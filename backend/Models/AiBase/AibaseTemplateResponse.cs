namespace Backend.Models.AiBase
{
    public class AibaseTemplateResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string Version { get; set; } = "1.0";
    }
}
