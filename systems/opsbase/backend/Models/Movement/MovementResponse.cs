namespace Backend.Models.Movement
{
    public class MovementResponse
    {
        public int Id { get; set; }
        public string Movementtype { get; set; }
        public string Status { get; set; }
        public int? Sourcelocationid { get; set; }
        public int? Targetlocationid { get; set; }
        public string? Referenceno { get; set; }
        public string? Notes { get; set; }
        public DateTime Operationat { get; set; }
        public string? Createdby { get; set; }
        public DateTime Createdat { get; set; }
    }
}
