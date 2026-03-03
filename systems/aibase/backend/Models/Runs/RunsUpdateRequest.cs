namespace Backend.Models.Runs
{
    public class RunsUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Projectid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string Runtype { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Status { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string? Enginerunid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Progresspct { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Requestedbyuserid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Triggersource { get; set; }
        public string? Inputjson { get; set; }
        public string? Outputjson { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(1000)]
        public string? Lasterror { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        public DateTime? Startedat { get; set; }
        public DateTime? Finishedat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
