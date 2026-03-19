namespace Backend.Models.Resourcedefinition
{
    public class ResourcedefinitionUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Codigo { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string Nombre { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(300)]
        public string? Descripcion { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Trackmode { get; set; }
        public int? Rubroid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
