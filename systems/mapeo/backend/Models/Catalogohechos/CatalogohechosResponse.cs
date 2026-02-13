namespace Backend.Models.Catalogohechos
{
    public class CatalogohechosResponse
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; }
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }
        public string? Palabrasclave { get; set; }
        public bool Activo { get; set; }
        public DateTime? Createdat { get; set; }
    }
}
