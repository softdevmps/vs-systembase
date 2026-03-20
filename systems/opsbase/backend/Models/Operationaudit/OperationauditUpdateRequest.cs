namespace Backend.Models.Operationaudit
{
    public class OperationauditUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Operationname { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Entityname { get; set; }
        public int? Entityid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(20)]
        public string Result { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Message { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Actor { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public Guid Correlationid { get; set; }
        public string? Payloadjson { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Executedat { get; set; }
    }
}
