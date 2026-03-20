namespace Backend.Models.OpsDashboard
{
    public class OpsDepositoCreateResponse
    {
        public int LocationId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "deposito";
        public int? RubroId { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
