namespace Backend.Models.Movementline
{
    public class MovementlineCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Movementid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourceinstanceid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Quantity { get; set; }
        public decimal? Unitcost { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string? Serie { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(120)]
        public string? Lote { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
    }
}
