namespace Backend.Models.Attributedefinition
{
    public class AttributedefinitionCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourcedefinitionid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Codigo { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string Nombre { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Datatype { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isrequired { get; set; }
        public int? Maxlength { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Sortorder { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Isactive { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
