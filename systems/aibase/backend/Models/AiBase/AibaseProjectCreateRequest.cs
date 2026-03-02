using System.ComponentModel.DataAnnotations;

namespace Backend.Models.AiBase
{
    public class AibaseProjectCreateRequest
    {
        [Required]
        [MaxLength(80)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "es";

        [MaxLength(100)]
        public string? Tone { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TemplateId { get; set; }
    }
}
