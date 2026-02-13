namespace Backend.Models.Catalogohechos
{
    public class CatalogohechosUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Codigo { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string Nombre { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Categoria { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Subcategoria { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string? Palabrasclave { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Activo { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
