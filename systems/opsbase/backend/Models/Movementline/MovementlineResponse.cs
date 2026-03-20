namespace Backend.Models.Movementline
{
    public class MovementlineResponse
    {
        public int Id { get; set; }
        public int Movementid { get; set; }
        public int Resourceinstanceid { get; set; }
        public decimal Quantity { get; set; }
        public decimal? Unitcost { get; set; }
        public string? Serie { get; set; }
        public string? Lote { get; set; }
        public DateTime Createdat { get; set; }
    }
}
