namespace Backend.Models.Templates
{
    public class TemplatesUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Key { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string Name { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Description { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Pipelinejson { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(20)]
        public string Version { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
