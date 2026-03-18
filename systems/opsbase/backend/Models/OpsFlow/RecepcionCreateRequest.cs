namespace Backend.Models.OpsFlow
{
    public class RecepcionCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourceinstanceid { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int Targetlocationid { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public decimal Quantity { get; set; }

        public decimal? Unitcost { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string? Referenceno { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime? Operationat { get; set; }

        public bool Confirmar { get; set; } = true;
    }
}
