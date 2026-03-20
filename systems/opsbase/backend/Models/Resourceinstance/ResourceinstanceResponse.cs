namespace Backend.Models.Resourceinstance
{
    public class ResourceinstanceResponse
    {
        public int Id { get; set; }
        public int Resourcedefinitionid { get; set; }
        public int? Rubroid { get; set; }
        public string? Rubrocodigo { get; set; }
        public string? Rubronombre { get; set; }
        public string? Rubrocolorhex { get; set; }
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
