namespace Backend.Models.Movement
{
    public class MovementCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Movementtype { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Status { get; set; }
        public int? Sourcelocationid { get; set; }
        public int? Targetlocationid { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string? Referenceno { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Notes { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Operationat { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Createdby { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
    }
}
