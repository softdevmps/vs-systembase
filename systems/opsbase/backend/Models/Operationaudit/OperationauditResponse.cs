namespace Backend.Models.Operationaudit
{
    public class OperationauditResponse
    {
        public int Id { get; set; }
        public string Operationname { get; set; }
        public string Entityname { get; set; }
        public int? Entityid { get; set; }
        public string Result { get; set; }
        public string? Message { get; set; }
        public string? Actor { get; set; }
        public Guid Correlationid { get; set; }
        public string? Payloadjson { get; set; }
        public DateTime Executedat { get; set; }
    }
}
