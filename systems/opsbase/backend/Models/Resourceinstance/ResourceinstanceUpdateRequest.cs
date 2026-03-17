namespace Backend.Models.Resourceinstance
{
    public class ResourceinstanceUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourcedefinitionid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string Codigointerno { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Estado { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string? Serie { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string? Lote { get; set; }
        public DateTime? Vencimiento { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
