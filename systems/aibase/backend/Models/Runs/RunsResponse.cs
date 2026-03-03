namespace Backend.Models.Runs
{
    public class RunsResponse
    {
        public int Id { get; set; }
        public int Projectid { get; set; }
        public string Runtype { get; set; }
        public string Status { get; set; }
        public string? Enginerunid { get; set; }
        public int Progresspct { get; set; }
        public int Requestedbyuserid { get; set; }
        public string Triggersource { get; set; }
        public string? Inputjson { get; set; }
        public string? Outputjson { get; set; }
        public string? Lasterror { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime? Startedat { get; set; }
        public DateTime? Finishedat { get; set; }
        public DateTime? Updatedat { get; set; }
    }
}
