namespace Backend.Models.OpsDashboard
{
    public class OpsDepositoContextResponse
    {
        public DateTime GeneratedAt { get; set; }
        public OpsDepositoLocationResponse Location { get; set; } = new();
        public OpsDepositoContextKpisResponse Kpis { get; set; } = new();

        public List<OpsDepositoStockRowResponse> StockItems { get; set; } = new();
        public List<OpsDepositoMovementRowResponse> PendingMovements { get; set; } = new();
        public List<OpsDepositoMovementRowResponse> RecentMovements { get; set; } = new();
        public List<OpsDepositoAuditRowResponse> RecentAudit { get; set; } = new();
    }

    public class OpsDepositoLocationResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? ParentLocationId { get; set; }
        public decimal? Capacidad { get; set; }

        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public string CoordinateMode { get; set; } = "db";
    }

    public class OpsDepositoContextKpisResponse
    {
        public int TotalMovements { get; set; }
        public int PendingMovements { get; set; }
        public int ConfirmedMovements { get; set; }
        public int StockCriticoItems { get; set; }
        public decimal StockDisponibleTotal { get; set; }
    }

    public class OpsDepositoStockRowResponse
    {
        public int StockBalanceId { get; set; }
        public int ResourceInstanceId { get; set; }
        public string ResourceCode { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string InstanceCode { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public decimal StockReal { get; set; }
        public decimal StockReservado { get; set; }
        public decimal StockDisponible { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class OpsDepositoMovementRowResponse
    {
        public int MovementId { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReferenceNo { get; set; }
        public DateTime? OperationAt { get; set; }
        public string? CreatedBy { get; set; }

        public int? SourceLocationId { get; set; }
        public int? TargetLocationId { get; set; }
        public string SourceLabel { get; set; } = string.Empty;
        public string TargetLabel { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;

        public int LineCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class OpsDepositoAuditRowResponse
    {
        public int Id { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Actor { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }
}
