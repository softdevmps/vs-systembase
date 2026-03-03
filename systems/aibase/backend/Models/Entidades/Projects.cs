namespace Backend.Models.Entidades
{
    public class Projects
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Language { get; set; }
        public string? Tone { get; set; }
        public string Status { get; set; }
        public bool Isactive { get; set; }
        public int Templateid { get; set; }
        public int? Tenantid { get; set; }
        public int Createdbyuserid { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
