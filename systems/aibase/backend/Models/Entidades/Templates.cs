namespace Backend.Models.Entidades
{
    public class Templates
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Pipelinejson { get; set; }
        public bool Isactive { get; set; }
        public string Version { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
