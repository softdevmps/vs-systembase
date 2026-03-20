namespace Backend.Models.Rubro
{
    public class RubroCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(60)]
        public string Codigo { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string Nombre { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(300)]
        public string? Descripcion { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(20)]
        public string? Colorhex { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; } = true;

        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime? Updatedat { get; set; }
    }
}
