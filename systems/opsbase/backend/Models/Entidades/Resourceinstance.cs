namespace Backend.Models.Entidades
{
    public class Resourceinstance
    {
        public int Id { get; set; }
        public int Resourcedefinitionid { get; set; }
        public string Codigointerno { get; set; }
        public string Estado { get; set; }
        public string? Serie { get; set; }
        public string? Lote { get; set; }
        public DateTime? Vencimiento { get; set; }
        public bool Isactive { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
