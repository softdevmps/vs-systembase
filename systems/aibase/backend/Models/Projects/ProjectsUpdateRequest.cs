namespace Backend.Models.Projects
{
    public class ProjectsUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Slug { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string Name { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Description { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(10)]
        public string Language { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Tone { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Status { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Templateid { get; set; }
        public int? Tenantid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Createdbyuserid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
