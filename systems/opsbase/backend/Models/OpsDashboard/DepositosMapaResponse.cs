namespace Backend.Models.OpsDashboard
{
    public class OpsDepositosMapaResponse
    {
        public DateTime GeneratedAt { get; set; }
        public bool UsesSyntheticCoordinates { get; set; }
        public OpsDepositosMapaKpisResponse Kpis { get; set; } = new();
        public List<OpsRubroOptionResponse> Rubros { get; set; } = new();
        public List<OpsDepositoMarkerResponse> Depositos { get; set; } = new();
    }

    public class OpsDepositosMapaKpisResponse
    {
        public int TotalDepositos { get; set; }
        public int TotalActivos { get; set; }
        public int ConCoordenadasReales { get; set; }
        public int StockCritico { get; set; }
        public int Pendientes { get; set; }
        public decimal StockDisponibleTotal { get; set; }
    }

    public class OpsDepositoMarkerResponse
    {
        public int LocationId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? RubroId { get; set; }
        public string RubroCodigo { get; set; } = string.Empty;
        public string RubroNombre { get; set; } = string.Empty;
        public string RubroColorHex { get; set; } = string.Empty;
        public int? ParentLocationId { get; set; }
        public decimal? Capacidad { get; set; }
        public bool IsActive { get; set; }

        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public string CoordinateMode { get; set; } = "db";

        public int ResourceItems { get; set; }
        public decimal StockReal { get; set; }
        public decimal StockReservado { get; set; }
        public decimal StockDisponible { get; set; }

        public int PendingMovements { get; set; }
        public int ConfirmedToday { get; set; }
        public DateTime? LastOperationAt { get; set; }
    }

    public class OpsRubroOptionResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public int Depositos { get; set; }
    }
}
