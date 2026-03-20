namespace Backend.Models.Entidades
{
    public class Resourcedefinition
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string Trackmode { get; set; }
        public bool Isactive { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
