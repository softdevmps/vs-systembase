namespace Backend.Models.Attributedefinition
{
    public class AttributedefinitionResponse
    {
        public int Id { get; set; }
        public int Resourcedefinitionid { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Datatype { get; set; }
        public bool Isrequired { get; set; }
        public int? Maxlength { get; set; }
        public int Sortorder { get; set; }
        public bool Isactive { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
