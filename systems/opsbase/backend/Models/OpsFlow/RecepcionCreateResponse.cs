namespace Backend.Models.OpsFlow
{
    public class RecepcionCreateResponse
    {
        public int Movementid { get; set; }
        public int Movementlineid { get; set; }
        public string Status { get; set; } = "borrador";
        public string Referenceno { get; set; } = string.Empty;
        public DateTime Operationat { get; set; }
        public decimal Quantity { get; set; }
        public int Resourceinstanceid { get; set; }
        public int Targetlocationid { get; set; }
    }
}
