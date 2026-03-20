namespace Backend.Models.OpsDashboard
{
    public class OpsDepositoDeleteResponse
    {
        public int LocationId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
