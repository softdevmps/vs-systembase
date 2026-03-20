namespace Backend.Models.Entidades
{
    public class Attributevalue
    {
        public int Id { get; set; }
        public int Resourceinstanceid { get; set; }
        public int Attributedefinitionid { get; set; }
        public string? Valuestring { get; set; }
        public decimal? Valuenumber { get; set; }
        public DateTime? Valuedatetime { get; set; }
        public bool? Valuebool { get; set; }
        public string? Valuejson { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
