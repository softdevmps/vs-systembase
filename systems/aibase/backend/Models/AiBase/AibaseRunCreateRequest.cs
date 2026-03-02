using System.ComponentModel.DataAnnotations;

namespace Backend.Models.AiBase
{
    public class AibaseRunCreateRequest
    {
        [Required]
        [MaxLength(50)]
        public string RunType { get; set; } = "dataset_build";

        public string? InputJson { get; set; }
    }
}
