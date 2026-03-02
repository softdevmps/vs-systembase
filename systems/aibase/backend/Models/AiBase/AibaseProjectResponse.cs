namespace Backend.Models.AiBase
{
    public class AibaseProjectResponse
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Language { get; set; } = "es";
        public string? Tone { get; set; }
        public string Status { get; set; } = "draft";
        public bool IsActive { get; set; }
        public int TemplateId { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
