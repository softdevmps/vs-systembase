namespace Backend.Models.Attributevalue
{
    public class AttributevalueCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourceinstanceid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Attributedefinitionid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Valuestring { get; set; }
        public decimal? Valuenumber { get; set; }
        public DateTime? Valuedatetime { get; set; }
        public bool? Valuebool { get; set; }
        public string? Valuejson { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
