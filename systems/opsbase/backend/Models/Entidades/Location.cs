namespace Backend.Models.Entidades
{
    public class Location
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public int? Parentlocationid { get; set; }
        public decimal? Capacidad { get; set; }
        public bool Isactive { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
