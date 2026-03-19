namespace Backend.Models.OpsDashboard
{
    public class OpsDepositoCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(80)]
        public string Codigo { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(160)]
        public string Nombre { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.MaxLength(30)]
        public string Tipo { get; set; } = "deposito";

        public int? RubroId { get; set; }
        public int? ParentLocationId { get; set; }
        public decimal? Capacidad { get; set; }
        public bool IsActive { get; set; } = true;

        [System.ComponentModel.DataAnnotations.Range(-90, 90)]
        public decimal Lat { get; set; }

        [System.ComponentModel.DataAnnotations.Range(-180, 180)]
        public decimal Lng { get; set; }
    }
}
