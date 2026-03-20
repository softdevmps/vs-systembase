using Backend.Data;
using Backend.Models.OpsDashboard;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class OpsDashboardGestor
    {
        private const string Schema = "sys_opsbase";
        private const double SyntheticBaseLat = -31.416667d;
        private const double SyntheticBaseLng = -64.183333d;
        private static readonly string[] LatCoordinateCandidates = { "Lat", "Latitude", "Latitud", "GeoLat", "MapLat" };
        private static readonly string[] LngCoordinateCandidates = { "Lng", "Longitude", "Longitud", "Lon", "GeoLng", "MapLng" };

        public static OpsDepositosMapaResponse ObtenerDepositosMapa(int? rubroId = null)
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            var latColumn = ResolveCoordinateColumn(conn, LatCoordinateCandidates);
            var lngColumn = ResolveCoordinateColumn(conn, LngCoordinateCandidates);

            var latExpr = latColumn == null
                ? "CAST(NULL AS decimal(10,6))"
                : $"TRY_CONVERT(decimal(10,6), l.[{latColumn}])";
            var lngExpr = lngColumn == null
                ? "CAST(NULL AS decimal(10,6))"
                : $"TRY_CONVERT(decimal(10,6), l.[{lngColumn}])";

            var sql = $@"
WITH stock AS (
    SELECT
        [LocationId],
        COUNT(1) AS [ResourceItems],
        CAST(ISNULL(SUM([StockReal]), 0) AS decimal(18,3)) AS [StockReal],
        CAST(ISNULL(SUM([StockReservado]), 0) AS decimal(18,3)) AS [StockReservado],
        CAST(ISNULL(SUM([StockDisponible]), 0) AS decimal(18,3)) AS [StockDisponible]
    FROM [{Schema}].[StockBalance]
    GROUP BY [LocationId]
),
pending_raw AS (
    SELECT [SourceLocationId] AS [LocationId]
    FROM [{Schema}].[Movement]
    WHERE LOWER(ISNULL([Status], '')) = 'borrador' AND [SourceLocationId] IS NOT NULL
    UNION ALL
    SELECT [TargetLocationId] AS [LocationId]
    FROM [{Schema}].[Movement]
    WHERE LOWER(ISNULL([Status], '')) = 'borrador' AND [TargetLocationId] IS NOT NULL
),
pending AS (
    SELECT [LocationId], COUNT(1) AS [PendingMovements]
    FROM pending_raw
    GROUP BY [LocationId]
),
confirmed_raw AS (
    SELECT [SourceLocationId] AS [LocationId]
    FROM [{Schema}].[Movement]
    WHERE LOWER(ISNULL([Status], '')) = 'confirmado'
      AND [OperationAt] >= CONVERT(date, GETUTCDATE())
      AND [SourceLocationId] IS NOT NULL
    UNION ALL
    SELECT [TargetLocationId] AS [LocationId]
    FROM [{Schema}].[Movement]
    WHERE LOWER(ISNULL([Status], '')) = 'confirmado'
      AND [OperationAt] >= CONVERT(date, GETUTCDATE())
      AND [TargetLocationId] IS NOT NULL
),
confirmed_today AS (
    SELECT [LocationId], COUNT(1) AS [ConfirmedToday]
    FROM confirmed_raw
    GROUP BY [LocationId]
),
lastop_raw AS (
    SELECT [SourceLocationId] AS [LocationId], [OperationAt]
    FROM [{Schema}].[Movement]
    WHERE [SourceLocationId] IS NOT NULL
    UNION ALL
    SELECT [TargetLocationId] AS [LocationId], [OperationAt]
    FROM [{Schema}].[Movement]
    WHERE [TargetLocationId] IS NOT NULL
),
lastop AS (
    SELECT [LocationId], MAX([OperationAt]) AS [LastOperationAt]
    FROM lastop_raw
    GROUP BY [LocationId]
)
SELECT
    l.[Id],
    l.[Codigo],
    l.[Nombre],
    l.[Tipo],
    l.[RubroId],
    ISNULL(rb.[Codigo], '') AS [RubroCodigo],
    ISNULL(rb.[Nombre], '') AS [RubroNombre],
    ISNULL(rb.[ColorHex], '') AS [RubroColorHex],
    l.[ParentLocationId],
    l.[Capacidad],
    l.[IsActive],
    {latExpr} AS [Lat],
    {lngExpr} AS [Lng],
    ISNULL(s.[ResourceItems], 0) AS [ResourceItems],
    ISNULL(s.[StockReal], 0) AS [StockReal],
    ISNULL(s.[StockReservado], 0) AS [StockReservado],
    ISNULL(s.[StockDisponible], 0) AS [StockDisponible],
    ISNULL(p.[PendingMovements], 0) AS [PendingMovements],
    ISNULL(c.[ConfirmedToday], 0) AS [ConfirmedToday],
    lo.[LastOperationAt]
FROM [{Schema}].[Location] l
LEFT JOIN [{Schema}].[Rubro] rb ON rb.[Id] = l.[RubroId]
LEFT JOIN stock s ON s.[LocationId] = l.[Id]
LEFT JOIN pending p ON p.[LocationId] = l.[Id]
LEFT JOIN confirmed_today c ON c.[LocationId] = l.[Id]
LEFT JOIN lastop lo ON lo.[LocationId] = l.[Id]
WHERE l.[IsActive] = 1
  AND (@RubroId IS NULL OR l.[RubroId] = @RubroId)
ORDER BY l.[Tipo] ASC, l.[Nombre] ASC, l.[Id] ASC;";

            var rows = new List<OpsDepositoMarkerResponse>();
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@RubroId", rubroId ?? (object)DBNull.Value);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new OpsDepositoMarkerResponse
                    {
                        LocationId = Convert.ToInt32(reader["Id"]),
                        Codigo = reader["Codigo"] == DBNull.Value ? string.Empty : reader["Codigo"].ToString() ?? string.Empty,
                        Nombre = reader["Nombre"] == DBNull.Value ? string.Empty : reader["Nombre"].ToString() ?? string.Empty,
                        Tipo = reader["Tipo"] == DBNull.Value ? string.Empty : reader["Tipo"].ToString() ?? string.Empty,
                        RubroId = reader["RubroId"] == DBNull.Value ? null : Convert.ToInt32(reader["RubroId"]),
                        RubroCodigo = reader["RubroCodigo"] == DBNull.Value ? string.Empty : reader["RubroCodigo"].ToString() ?? string.Empty,
                        RubroNombre = reader["RubroNombre"] == DBNull.Value ? string.Empty : reader["RubroNombre"].ToString() ?? string.Empty,
                        RubroColorHex = reader["RubroColorHex"] == DBNull.Value ? string.Empty : reader["RubroColorHex"].ToString() ?? string.Empty,
                        ParentLocationId = reader["ParentLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["ParentLocationId"]),
                        Capacidad = ToNullableDecimal(reader["Capacidad"]),
                        IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                        Lat = ToNullableDecimal(reader["Lat"]),
                        Lng = ToNullableDecimal(reader["Lng"]),
                        ResourceItems = reader["ResourceItems"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ResourceItems"]),
                        StockReal = ToDecimal(reader["StockReal"]),
                        StockReservado = ToDecimal(reader["StockReservado"]),
                        StockDisponible = ToDecimal(reader["StockDisponible"]),
                        PendingMovements = reader["PendingMovements"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PendingMovements"]),
                        ConfirmedToday = reader["ConfirmedToday"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ConfirmedToday"]),
                        LastOperationAt = reader["LastOperationAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastOperationAt"])
                    });
                }
            }

            var depositos = rows.Where(r => IsDepositType(r.Tipo)).ToList();
            if (depositos.Count == 0)
                depositos = rows;

            ApplyCoordinates(depositos);

            var response = new OpsDepositosMapaResponse
            {
                GeneratedAt = DateTime.UtcNow,
                UsesSyntheticCoordinates = depositos.Any(d => d.CoordinateMode == "synthetic"),
                Rubros = ObtenerRubros(conn),
                Kpis = new OpsDepositosMapaKpisResponse
                {
                    TotalDepositos = depositos.Count,
                    TotalActivos = depositos.Count(d => d.IsActive),
                    ConCoordenadasReales = depositos.Count(d => d.CoordinateMode == "db"),
                    StockCritico = depositos.Count(d => d.StockDisponible <= 0),
                    Pendientes = depositos.Sum(d => d.PendingMovements),
                    StockDisponibleTotal = depositos.Sum(d => d.StockDisponible)
                },
                Depositos = depositos
            };

            return response;
        }

        public static (bool Ok, string? Error, OpsDepositoContextResponse? Data) ObtenerDepositoContexto(int locationId, int limit)
        {
            if (locationId <= 0)
                return (false, "LocationId inválido.", null);

            var safeLimit = Math.Clamp(limit, 10, 200);

            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            var latColumn = ResolveCoordinateColumn(conn, LatCoordinateCandidates);
            var lngColumn = ResolveCoordinateColumn(conn, LngCoordinateCandidates);

            var latExpr = latColumn == null
                ? "CAST(NULL AS decimal(10,6))"
                : $"TRY_CONVERT(decimal(10,6), l.[{latColumn}])";
            var lngExpr = lngColumn == null
                ? "CAST(NULL AS decimal(10,6))"
                : $"TRY_CONVERT(decimal(10,6), l.[{lngColumn}])";

            OpsDepositoLocationResponse? location = null;

            var sqlLocation = $@"SELECT TOP 1
    l.[Id], l.[Codigo], l.[Nombre], l.[Tipo], l.[RubroId], l.[IsActive], l.[ParentLocationId], l.[Capacidad],
    ISNULL(rb.[Codigo], '') AS [RubroCodigo],
    ISNULL(rb.[Nombre], '') AS [RubroNombre],
    ISNULL(rb.[ColorHex], '') AS [RubroColorHex],
    {latExpr} AS [Lat], {lngExpr} AS [Lng]
FROM [{Schema}].[Location] l
LEFT JOIN [{Schema}].[Rubro] rb ON rb.[Id] = l.[RubroId]
WHERE l.[Id] = @id;";

            using (var cmd = new SqlCommand(sqlLocation, conn))
            {
                cmd.Parameters.AddWithValue("@id", locationId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    location = new OpsDepositoLocationResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Codigo = reader["Codigo"] == DBNull.Value ? string.Empty : reader["Codigo"].ToString() ?? string.Empty,
                        Nombre = reader["Nombre"] == DBNull.Value ? string.Empty : reader["Nombre"].ToString() ?? string.Empty,
                        Tipo = reader["Tipo"] == DBNull.Value ? string.Empty : reader["Tipo"].ToString() ?? string.Empty,
                        RubroId = reader["RubroId"] == DBNull.Value ? null : Convert.ToInt32(reader["RubroId"]),
                        RubroCodigo = reader["RubroCodigo"] == DBNull.Value ? string.Empty : reader["RubroCodigo"].ToString() ?? string.Empty,
                        RubroNombre = reader["RubroNombre"] == DBNull.Value ? string.Empty : reader["RubroNombre"].ToString() ?? string.Empty,
                        RubroColorHex = reader["RubroColorHex"] == DBNull.Value ? string.Empty : reader["RubroColorHex"].ToString() ?? string.Empty,
                        IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                        ParentLocationId = reader["ParentLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["ParentLocationId"]),
                        Capacidad = ToNullableDecimal(reader["Capacidad"]),
                        Lat = ToNullableDecimal(reader["Lat"]),
                        Lng = ToNullableDecimal(reader["Lng"])
                    };
                }
            }

            if (location == null)
                return (false, "Depósito no encontrado.", null);

            location.CoordinateMode = location.Lat.HasValue && location.Lng.HasValue ? "db" : "synthetic";

            if (location.CoordinateMode == "synthetic")
            {
                var synthetic = BuildSyntheticCoordinateFor(location.Id);
                location.Lat = synthetic.lat;
                location.Lng = synthetic.lng;
            }

            var kpis = new OpsDepositoContextKpisResponse();
            const string sqlKpis = @"SELECT
    (SELECT COUNT(1) FROM [sys_opsbase].[Movement] WHERE [SourceLocationId] = @id OR [TargetLocationId] = @id) AS [TotalMovements],
    (SELECT COUNT(1) FROM [sys_opsbase].[Movement] WHERE ([SourceLocationId] = @id OR [TargetLocationId] = @id) AND LOWER(ISNULL([Status], '')) = 'borrador') AS [PendingMovements],
    (SELECT COUNT(1) FROM [sys_opsbase].[Movement] WHERE ([SourceLocationId] = @id OR [TargetLocationId] = @id) AND LOWER(ISNULL([Status], '')) = 'confirmado') AS [ConfirmedMovements],
    (SELECT COUNT(1) FROM [sys_opsbase].[StockBalance] WHERE [LocationId] = @id AND [StockDisponible] <= 0) AS [StockCriticoItems],
    (SELECT CAST(ISNULL(SUM([StockDisponible]), 0) AS decimal(18,3)) FROM [sys_opsbase].[StockBalance] WHERE [LocationId] = @id) AS [StockDisponibleTotal];";

            using (var cmd = new SqlCommand(sqlKpis, conn))
            {
                cmd.Parameters.AddWithValue("@id", locationId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    kpis.TotalMovements = reader["TotalMovements"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalMovements"]);
                    kpis.PendingMovements = reader["PendingMovements"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PendingMovements"]);
                    kpis.ConfirmedMovements = reader["ConfirmedMovements"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ConfirmedMovements"]);
                    kpis.StockCriticoItems = reader["StockCriticoItems"] == DBNull.Value ? 0 : Convert.ToInt32(reader["StockCriticoItems"]);
                    kpis.StockDisponibleTotal = ToDecimal(reader["StockDisponibleTotal"]);
                }
            }

            var stockItems = ObtenerStockItems(conn, locationId, safeLimit);
            var pendingMovements = ObtenerMovements(conn, locationId, safeLimit, onlyPending: true);
            var recentMovements = ObtenerMovements(conn, locationId, safeLimit, onlyPending: false);
            var auditRows = ObtenerAuditRows(conn, locationId, safeLimit);

            var response = new OpsDepositoContextResponse
            {
                GeneratedAt = DateTime.UtcNow,
                Location = location,
                Kpis = kpis,
                StockItems = stockItems,
                PendingMovements = pendingMovements,
                RecentMovements = recentMovements,
                RecentAudit = auditRows
            };

            return (true, null, response);
        }

        public static (bool Ok, string? Error, OpsDepositoCreateResponse? Data) CrearDeposito(
            OpsDepositoCreateRequest request,
            string? actor = null)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo))
                return (false, "Codigo es requerido.", null);
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return (false, "Nombre es requerido.", null);
            if (!request.RubroId.HasValue || request.RubroId.Value <= 0)
                return (false, "Rubro es requerido.", null);

            var codigo = request.Codigo.Trim();
            var nombre = request.Nombre.Trim();
            var tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "deposito" : request.Tipo.Trim().ToLowerInvariant();

            if (codigo.Length > 80)
                return (false, "Codigo excede 80 caracteres.", null);
            if (nombre.Length > 160)
                return (false, "Nombre excede 160 caracteres.", null);
            if (tipo.Length > 30)
                return (false, "Tipo excede 30 caracteres.", null);
            if (request.Lat < -90 || request.Lat > 90)
                return (false, "Lat fuera de rango.", null);
            if (request.Lng < -180 || request.Lng > 180)
                return (false, "Lng fuera de rango.", null);

            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            EnsureCoordinateColumns(conn);

            using var tx = conn.BeginTransaction();
            try
            {
                if (LocationCodeExists(conn, tx, codigo))
                {
                    tx.Rollback();
                    return (false, "Ya existe un depósito con ese código.", null);
                }

                if (request.ParentLocationId.HasValue && !LocationExists(conn, tx, request.ParentLocationId.Value))
                {
                    tx.Rollback();
                    return (false, "ParentLocationId no existe.", null);
                }
                if (!RubroSchemaHelper.ExistsActiveRubro(conn, request.RubroId.Value, tx))
                {
                    tx.Rollback();
                    return (false, "Rubro inexistente o inactivo (RubroId).", null);
                }

                var now = DateTime.UtcNow;
                var createdBy = string.IsNullOrWhiteSpace(actor) ? "runtime-ui" : actor.Trim();

const string sql = @"INSERT INTO [sys_opsbase].[Location]
([Codigo], [Nombre], [Tipo], [RubroId], [ParentLocationId], [Capacidad], [IsActive], [Lat], [Lng], [CreatedAt], [UpdatedAt])
VALUES
(@Codigo, @Nombre, @Tipo, @RubroId, @ParentLocationId, @Capacidad, @IsActive, @Lat, @Lng, @CreatedAt, @UpdatedAt);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int locationId;
                using (var cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.Parameters.AddWithValue("@RubroId", request.RubroId.Value);
                    cmd.Parameters.AddWithValue("@ParentLocationId", request.ParentLocationId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Capacidad", request.Capacidad ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                    cmd.Parameters.AddWithValue("@Lat", request.Lat);
                    cmd.Parameters.AddWithValue("@Lng", request.Lng);
                    cmd.Parameters.AddWithValue("@CreatedAt", now);
                    cmd.Parameters.AddWithValue("@UpdatedAt", now);
                    locationId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                SyncCoordinatesToAllColumns(conn, tx, locationId, request.Lat, request.Lng);

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "ops.deposito.create",
                    entityName: "Location",
                    entityId: locationId,
                    result: "ok",
                    message: "Depósito creado con geolocalización.",
                    actor: createdBy,
                    payload: new
                    {
                        locationId,
                        codigo,
                        nombre,
                        tipo,
                        request.RubroId,
                        request.ParentLocationId,
                        request.Capacidad,
                        request.IsActive,
                        request.Lat,
                        request.Lng
                    });

                tx.Commit();

                return (true, null, new OpsDepositoCreateResponse
                {
                    LocationId = locationId,
                    Codigo = codigo,
                    Nombre = nombre,
                    Tipo = tipo,
                    RubroId = request.RubroId,
                    Lat = request.Lat,
                    Lng = request.Lng,
                    IsActive = request.IsActive,
                    CreatedAt = now
                });
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al crear depósito: {ex.Message}", null);
            }
        }

        public static (bool Ok, string? Error, OpsDepositoCreateResponse? Data) EditarDeposito(
            int locationId,
            OpsDepositoUpdateRequest request,
            string? actor = null)
        {
            if (locationId <= 0)
                return (false, "LocationId inválido.", null);
            if (string.IsNullOrWhiteSpace(request.Codigo))
                return (false, "Codigo es requerido.", null);
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return (false, "Nombre es requerido.", null);
            if (!request.RubroId.HasValue || request.RubroId.Value <= 0)
                return (false, "Rubro es requerido.", null);

            var codigo = request.Codigo.Trim();
            var nombre = request.Nombre.Trim();
            var tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "deposito" : request.Tipo.Trim().ToLowerInvariant();

            if (codigo.Length > 80)
                return (false, "Codigo excede 80 caracteres.", null);
            if (nombre.Length > 160)
                return (false, "Nombre excede 160 caracteres.", null);
            if (tipo.Length > 30)
                return (false, "Tipo excede 30 caracteres.", null);
            if (request.Lat < -90 || request.Lat > 90)
                return (false, "Lat fuera de rango.", null);
            if (request.Lng < -180 || request.Lng > 180)
                return (false, "Lng fuera de rango.", null);
            if (request.ParentLocationId == locationId)
                return (false, "ParentLocationId no puede ser el mismo depósito.", null);

            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            EnsureCoordinateColumns(conn);

            using var tx = conn.BeginTransaction();
            try
            {
                if (!LocationExists(conn, tx, locationId))
                {
                    tx.Rollback();
                    return (false, "Depósito no encontrado.", null);
                }

                if (LocationCodeExists(conn, tx, codigo, excludeLocationId: locationId))
                {
                    tx.Rollback();
                    return (false, "Ya existe un depósito con ese código.", null);
                }

                if (request.ParentLocationId.HasValue && !LocationExists(conn, tx, request.ParentLocationId.Value))
                {
                    tx.Rollback();
                    return (false, "ParentLocationId no existe.", null);
                }
                if (!RubroSchemaHelper.ExistsActiveRubro(conn, request.RubroId.Value, tx))
                {
                    tx.Rollback();
                    return (false, "Rubro inexistente o inactivo (RubroId).", null);
                }

                var now = DateTime.UtcNow;
                var createdBy = string.IsNullOrWhiteSpace(actor) ? "runtime-ui" : actor.Trim();

                const string sql = @"UPDATE [sys_opsbase].[Location]
SET [Codigo] = @Codigo,
    [Nombre] = @Nombre,
    [Tipo] = @Tipo,
    [RubroId] = @RubroId,
    [ParentLocationId] = @ParentLocationId,
    [Capacidad] = @Capacidad,
    [IsActive] = @IsActive,
    [Lat] = @Lat,
    [Lng] = @Lng,
    [UpdatedAt] = @UpdatedAt
WHERE [Id] = @Id;";

                using (var cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", locationId);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.Parameters.AddWithValue("@RubroId", request.RubroId.Value);
                    cmd.Parameters.AddWithValue("@ParentLocationId", request.ParentLocationId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Capacidad", request.Capacidad ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                    cmd.Parameters.AddWithValue("@Lat", request.Lat);
                    cmd.Parameters.AddWithValue("@Lng", request.Lng);
                    cmd.Parameters.AddWithValue("@UpdatedAt", now);
                    cmd.ExecuteNonQuery();
                }

                SyncCoordinatesToAllColumns(conn, tx, locationId, request.Lat, request.Lng);

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "ops.deposito.update",
                    entityName: "Location",
                    entityId: locationId,
                    result: "ok",
                    message: "Depósito actualizado con geolocalización.",
                    actor: createdBy,
                    payload: new
                    {
                        locationId,
                        codigo,
                        nombre,
                        tipo,
                        request.RubroId,
                        request.ParentLocationId,
                        request.Capacidad,
                        request.IsActive,
                        request.Lat,
                        request.Lng
                    });

                tx.Commit();

                return (true, null, new OpsDepositoCreateResponse
                {
                    LocationId = locationId,
                    Codigo = codigo,
                    Nombre = nombre,
                    Tipo = tipo,
                    RubroId = request.RubroId,
                    Lat = request.Lat,
                    Lng = request.Lng,
                    IsActive = request.IsActive,
                    CreatedAt = now
                });
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al editar depósito: {ex.Message}", null);
            }
        }

        public static (bool Ok, string? Error, OpsDepositoDeleteResponse? Data) EliminarDeposito(
            int locationId,
            string? actor = null)
        {
            if (locationId <= 0)
                return (false, "LocationId inválido.", null);

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                string codigo = string.Empty;
                string nombre = string.Empty;
                bool isActive;

                const string sqlLocation = @"SELECT TOP 1 [Codigo], [Nombre], [IsActive]
FROM [sys_opsbase].[Location]
WHERE [Id] = @id;";
                using (var cmd = new SqlCommand(sqlLocation, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", locationId);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        tx.Rollback();
                        return (false, "Depósito no encontrado.", null);
                    }

                    codigo = reader["Codigo"] == DBNull.Value ? string.Empty : reader["Codigo"].ToString() ?? string.Empty;
                    nombre = reader["Nombre"] == DBNull.Value ? string.Empty : reader["Nombre"].ToString() ?? string.Empty;
                    isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
                }

                if (!isActive)
                {
                    tx.Rollback();
                    return (false, "El depósito ya está inactivo.", null);
                }

                if (HasActiveChildLocations(conn, tx, locationId))
                {
                    tx.Rollback();
                    return (false, "No se puede eliminar: tiene ubicaciones hijas activas.", null);
                }

                var now = DateTime.UtcNow;
                var deletedBy = string.IsNullOrWhiteSpace(actor) ? "runtime-ui" : actor.Trim();

                const string sqlUpdate = @"UPDATE [sys_opsbase].[Location]
SET [IsActive] = 0,
    [UpdatedAt] = @UpdatedAt
WHERE [Id] = @Id
  AND [IsActive] = 1;";
                using (var cmd = new SqlCommand(sqlUpdate, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", locationId);
                    cmd.Parameters.AddWithValue("@UpdatedAt", now);
                    var rows = cmd.ExecuteNonQuery();
                    if (rows <= 0)
                    {
                        tx.Rollback();
                        return (false, "No se pudo eliminar el depósito.", null);
                    }
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "ops.deposito.delete",
                    entityName: "Location",
                    entityId: locationId,
                    result: "ok",
                    message: "Depósito desactivado desde Ops Dashboard.",
                    actor: deletedBy,
                    payload: new
                    {
                        locationId,
                        codigo,
                        nombre
                    });

                tx.Commit();

                return (true, null, new OpsDepositoDeleteResponse
                {
                    LocationId = locationId,
                    Codigo = codigo,
                    Nombre = nombre,
                    IsActive = false,
                    DeletedAt = now
                });
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al eliminar depósito: {ex.Message}", null);
            }
        }

        private static List<OpsDepositoStockRowResponse> ObtenerStockItems(SqlConnection conn, int locationId, int limit)
        {
            const string sql = @"SELECT TOP (@limit)
    sb.[Id] AS [StockBalanceId],
    sb.[ResourceInstanceId],
    ISNULL(rd.[Codigo], '') AS [ResourceCode],
    ISNULL(rd.[Nombre], '') AS [ResourceName],
    rd.[RubroId],
    ISNULL(rb.[Codigo], '') AS [RubroCodigo],
    ISNULL(rb.[Nombre], '') AS [RubroNombre],
    ISNULL(ri.[CodigoInterno], '') AS [InstanceCode],
    ISNULL(ri.[Estado], '') AS [Estado],
    sb.[StockReal],
    sb.[StockReservado],
    sb.[StockDisponible],
    sb.[UpdatedAt]
FROM [sys_opsbase].[StockBalance] sb
LEFT JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = sb.[ResourceInstanceId]
LEFT JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ri.[ResourceDefinitionId]
LEFT JOIN [sys_opsbase].[Rubro] rb ON rb.[Id] = rd.[RubroId]
WHERE sb.[LocationId] = @locationId
ORDER BY sb.[StockDisponible] ASC, sb.[StockReservado] DESC, sb.[Id] ASC;";

            var rows = new List<OpsDepositoStockRowResponse>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new OpsDepositoStockRowResponse
                {
                    StockBalanceId = Convert.ToInt32(reader["StockBalanceId"]),
                    ResourceInstanceId = Convert.ToInt32(reader["ResourceInstanceId"]),
                    ResourceCode = reader["ResourceCode"] == DBNull.Value ? string.Empty : reader["ResourceCode"].ToString() ?? string.Empty,
                    ResourceName = reader["ResourceName"] == DBNull.Value ? string.Empty : reader["ResourceName"].ToString() ?? string.Empty,
                    RubroId = reader["RubroId"] == DBNull.Value ? null : Convert.ToInt32(reader["RubroId"]),
                    RubroCodigo = reader["RubroCodigo"] == DBNull.Value ? string.Empty : reader["RubroCodigo"].ToString() ?? string.Empty,
                    RubroNombre = reader["RubroNombre"] == DBNull.Value ? string.Empty : reader["RubroNombre"].ToString() ?? string.Empty,
                    InstanceCode = reader["InstanceCode"] == DBNull.Value ? string.Empty : reader["InstanceCode"].ToString() ?? string.Empty,
                    Estado = reader["Estado"] == DBNull.Value ? string.Empty : reader["Estado"].ToString() ?? string.Empty,
                    StockReal = ToDecimal(reader["StockReal"]),
                    StockReservado = ToDecimal(reader["StockReservado"]),
                    StockDisponible = ToDecimal(reader["StockDisponible"]),
                    UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
                });
            }

            return rows;
        }

        private static List<OpsDepositoMovementRowResponse> ObtenerMovements(
            SqlConnection conn,
            int locationId,
            int limit,
            bool onlyPending)
        {
            var sql = @"SELECT TOP (@limit)
    m.[Id] AS [MovementId],
    m.[MovementType],
    m.[Status],
    m.[ReferenceNo],
    m.[OperationAt],
    m.[CreatedBy],
    m.[SourceLocationId],
    m.[TargetLocationId],
    ISNULL(src.[Codigo], '') AS [SourceCodigo],
    ISNULL(src.[Nombre], '') AS [SourceNombre],
    ISNULL(dst.[Codigo], '') AS [TargetCodigo],
    ISNULL(dst.[Nombre], '') AS [TargetNombre],
    ISNULL(lines.[LineCount], 0) AS [LineCount],
    CAST(ISNULL(lines.[TotalQuantity], 0) AS decimal(18,3)) AS [TotalQuantity]
FROM [sys_opsbase].[Movement] m
LEFT JOIN [sys_opsbase].[Location] src ON src.[Id] = m.[SourceLocationId]
LEFT JOIN [sys_opsbase].[Location] dst ON dst.[Id] = m.[TargetLocationId]
OUTER APPLY (
    SELECT COUNT(1) AS [LineCount], SUM([Quantity]) AS [TotalQuantity]
    FROM [sys_opsbase].[MovementLine] ml
    WHERE ml.[MovementId] = m.[Id]
) lines
WHERE (m.[SourceLocationId] = @locationId OR m.[TargetLocationId] = @locationId)
  AND " + (onlyPending
                ? "LOWER(ISNULL(m.[Status], '')) = 'borrador'"
                : "LOWER(ISNULL(m.[Status], '')) <> 'borrador'") + @"
ORDER BY m.[OperationAt] DESC, m.[Id] DESC;";

            var rows = new List<OpsDepositoMovementRowResponse>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int? sourceId = reader["SourceLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["SourceLocationId"]);
                int? targetId = reader["TargetLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["TargetLocationId"]);

                var sourceLabel = BuildLocationLabel(
                    sourceId,
                    reader["SourceCodigo"] == DBNull.Value ? null : reader["SourceCodigo"].ToString(),
                    reader["SourceNombre"] == DBNull.Value ? null : reader["SourceNombre"].ToString());
                var targetLabel = BuildLocationLabel(
                    targetId,
                    reader["TargetCodigo"] == DBNull.Value ? null : reader["TargetCodigo"].ToString(),
                    reader["TargetNombre"] == DBNull.Value ? null : reader["TargetNombre"].ToString());

                rows.Add(new OpsDepositoMovementRowResponse
                {
                    MovementId = Convert.ToInt32(reader["MovementId"]),
                    MovementType = reader["MovementType"] == DBNull.Value ? string.Empty : reader["MovementType"].ToString() ?? string.Empty,
                    Status = reader["Status"] == DBNull.Value ? string.Empty : reader["Status"].ToString() ?? string.Empty,
                    ReferenceNo = reader["ReferenceNo"] == DBNull.Value ? null : reader["ReferenceNo"].ToString(),
                    OperationAt = reader["OperationAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["OperationAt"]),
                    CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString(),
                    SourceLocationId = sourceId,
                    TargetLocationId = targetId,
                    SourceLabel = sourceLabel,
                    TargetLabel = targetLabel,
                    Direction = ResolveDirection(locationId, sourceId, targetId),
                    LineCount = reader["LineCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LineCount"]),
                    TotalQuantity = ToDecimal(reader["TotalQuantity"])
                });
            }

            return rows;
        }

        private static List<OpsDepositoAuditRowResponse> ObtenerAuditRows(SqlConnection conn, int locationId, int limit)
        {
            const string sql = @"SELECT TOP (@limit)
    oa.[Id],
    oa.[OperationName],
    oa.[EntityName],
    oa.[EntityId],
    oa.[Result],
    oa.[Message],
    oa.[Actor],
    oa.[ExecutedAt]
FROM [sys_opsbase].[OperationAudit] oa
WHERE EXISTS (
    SELECT 1
    FROM [sys_opsbase].[Movement] m
    WHERE oa.[EntityName] = 'Movement'
      AND oa.[EntityId] = m.[Id]
      AND (m.[SourceLocationId] = @locationId OR m.[TargetLocationId] = @locationId)
)
ORDER BY oa.[ExecutedAt] DESC, oa.[Id] DESC;";

            var rows = new List<OpsDepositoAuditRowResponse>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new OpsDepositoAuditRowResponse
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    OperationName = reader["OperationName"] == DBNull.Value ? string.Empty : reader["OperationName"].ToString() ?? string.Empty,
                    EntityName = reader["EntityName"] == DBNull.Value ? string.Empty : reader["EntityName"].ToString() ?? string.Empty,
                    EntityId = reader["EntityId"] == DBNull.Value ? null : Convert.ToInt32(reader["EntityId"]),
                    Result = reader["Result"] == DBNull.Value ? string.Empty : reader["Result"].ToString() ?? string.Empty,
                    Message = reader["Message"] == DBNull.Value ? null : reader["Message"].ToString(),
                    Actor = reader["Actor"] == DBNull.Value ? null : reader["Actor"].ToString(),
                    ExecutedAt = reader["ExecutedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["ExecutedAt"])
                });
            }

            return rows;
        }

        private static List<OpsRubroOptionResponse> ObtenerRubros(SqlConnection conn)
        {
            const string sql = @"SELECT
    rb.[Id],
    rb.[Codigo],
    rb.[Nombre],
    ISNULL(rb.[ColorHex], '') AS [ColorHex],
    ISNULL(loc.[Depositos], 0) AS [Depositos]
FROM [sys_opsbase].[Rubro] rb
LEFT JOIN (
    SELECT [RubroId], COUNT(1) AS [Depositos]
    FROM [sys_opsbase].[Location]
    WHERE [IsActive] = 1
      AND [RubroId] IS NOT NULL
    GROUP BY [RubroId]
) loc ON loc.[RubroId] = rb.[Id]
WHERE rb.[IsActive] = 1
ORDER BY rb.[Nombre] ASC;";

            var rows = new List<OpsRubroOptionResponse>();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new OpsRubroOptionResponse
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Codigo = reader["Codigo"] == DBNull.Value ? string.Empty : reader["Codigo"].ToString() ?? string.Empty,
                    Nombre = reader["Nombre"] == DBNull.Value ? string.Empty : reader["Nombre"].ToString() ?? string.Empty,
                    ColorHex = reader["ColorHex"] == DBNull.Value ? string.Empty : reader["ColorHex"].ToString() ?? string.Empty,
                    Depositos = reader["Depositos"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Depositos"])
                });
            }

            return rows;
        }

        private static void EnsureCoordinateColumns(SqlConnection conn)
        {
            const string sql = @"
IF COL_LENGTH('[sys_opsbase].[Location]', 'Lat') IS NULL
    ALTER TABLE [sys_opsbase].[Location] ADD [Lat] DECIMAL(10,6) NULL;

IF COL_LENGTH('[sys_opsbase].[Location]', 'Lng') IS NULL
    ALTER TABLE [sys_opsbase].[Location] ADD [Lng] DECIMAL(10,6) NULL;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static bool LocationCodeExists(SqlConnection conn, SqlTransaction tx, string codigo, int? excludeLocationId = null)
        {
            var sql = @"SELECT TOP 1 1
FROM [sys_opsbase].[Location]
WHERE LOWER(LTRIM(RTRIM([Codigo]))) = LOWER(LTRIM(RTRIM(@codigo)))";

            if (excludeLocationId.HasValue)
                sql += " AND [Id] <> @excludeId";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@codigo", codigo);
            if (excludeLocationId.HasValue)
                cmd.Parameters.AddWithValue("@excludeId", excludeLocationId.Value);
            return cmd.ExecuteScalar() != null;
        }

        private static bool LocationExists(SqlConnection conn, SqlTransaction tx, int locationId)
        {
            const string sql = @"SELECT TOP 1 1 FROM [sys_opsbase].[Location] WHERE [Id] = @id;";
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", locationId);
            return cmd.ExecuteScalar() != null;
        }

        private static bool HasActiveChildLocations(SqlConnection conn, SqlTransaction tx, int locationId)
        {
            const string sql = @"SELECT TOP 1 1
FROM [sys_opsbase].[Location]
WHERE [ParentLocationId] = @id
  AND [IsActive] = 1;";
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", locationId);
            return cmd.ExecuteScalar() != null;
        }

        private static bool HasColumn(SqlConnection conn, string tableName, string columnName)
        {
            const string sql = @"SELECT 1
FROM sys.columns
WHERE [object_id] = OBJECT_ID(@fullTableName)
  AND [name] = @columnName;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@fullTableName", $"[{Schema}].[{tableName}]");
            cmd.Parameters.AddWithValue("@columnName", columnName);
            return cmd.ExecuteScalar() != null;
        }

        private static bool HasWritableColumn(SqlConnection conn, SqlTransaction? tx, string tableName, string columnName)
        {
            const string sql = @"SELECT 1
FROM sys.columns
WHERE [object_id] = OBJECT_ID(@fullTableName)
  AND [name] = @columnName
  AND [is_computed] = 0;";

            using var cmd = tx == null
                ? new SqlCommand(sql, conn)
                : new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@fullTableName", $"[{Schema}].[{tableName}]");
            cmd.Parameters.AddWithValue("@columnName", columnName);
            return cmd.ExecuteScalar() != null;
        }

        private static string? ResolveCoordinateColumn(SqlConnection conn, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (HasColumn(conn, "Location", candidate))
                    return candidate;
            }

            return null;
        }

        private static void SyncCoordinatesToAllColumns(SqlConnection conn, SqlTransaction tx, int locationId, decimal lat, decimal lng)
        {
            var latColumns = LatCoordinateCandidates
                .Where(column => HasWritableColumn(conn, tx, "Location", column))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lngColumns = LngCoordinateCandidates
                .Where(column => HasWritableColumn(conn, tx, "Location", column))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (latColumns.Count == 0 && lngColumns.Count == 0)
                return;

            var assignments = new List<string>();
            assignments.AddRange(latColumns.Select(column => $"[{column}] = @Lat"));
            assignments.AddRange(lngColumns.Select(column => $"[{column}] = @Lng"));

            var sql = $@"UPDATE [{Schema}].[Location]
SET {string.Join(", ", assignments)}
WHERE [Id] = @Id;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Id", locationId);
            cmd.Parameters.AddWithValue("@Lat", lat);
            cmd.Parameters.AddWithValue("@Lng", lng);
            cmd.ExecuteNonQuery();
        }

        private static bool IsDepositType(string? type)
        {
            var key = (type ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key)) return false;

            return key.Contains("deposit")
                || key.Contains("almacen")
                || key.Contains("warehouse")
                || key.Contains("hub")
                || key.Contains("sucursal")
                || key.Contains("nodo")
                || key.Contains("centro");
        }

        private static void ApplyCoordinates(List<OpsDepositoMarkerResponse> items)
        {
            foreach (var item in items)
            {
                if (item.Lat.HasValue && item.Lng.HasValue)
                {
                    item.CoordinateMode = "db";
                    continue;
                }

                var synthetic = BuildSyntheticCoordinateFor(item.LocationId, baseLat: SyntheticBaseLat, baseLng: SyntheticBaseLng);
                item.Lat = synthetic.lat;
                item.Lng = synthetic.lng;
                item.CoordinateMode = "synthetic";
            }
        }

        private static (decimal lat, decimal lng) BuildSyntheticCoordinateFor(int locationId, int index = 0, double? baseLat = null, double? baseLng = null)
        {
            var originLat = baseLat ?? SyntheticBaseLat;
            var originLng = baseLng ?? SyntheticBaseLng;

            // Coordenadas sintéticas estables por depósito (no dependen del resto).
            var hash = Math.Abs(unchecked(locationId * 265443576 + 1013904223));
            var row = (hash / 11) % 7; // 0..6
            var col = (hash / 17) % 7; // 0..6
            var latJitter = (hash % 100) / 10000d;
            var lngJitter = ((hash / 100) % 100) / 10000d;

            var lat = originLat + ((row - 3) * 0.018d) + latJitter;
            var lng = originLng + ((col - 3) * 0.022d) + lngJitter;

            return (Math.Round((decimal)lat, 6), Math.Round((decimal)lng, 6));
        }

        private static string BuildLocationLabel(int? id, string? code, string? name)
        {
            if (id == null) return "—";

            var codeTrim = (code ?? string.Empty).Trim();
            var nameTrim = (name ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(codeTrim) && !string.IsNullOrWhiteSpace(nameTrim))
                return $"{codeTrim} · {nameTrim}";
            if (!string.IsNullOrWhiteSpace(nameTrim))
                return nameTrim;
            if (!string.IsNullOrWhiteSpace(codeTrim))
                return codeTrim;

            return $"#{id.Value}";
        }

        private static string ResolveDirection(int locationId, int? sourceId, int? targetId)
        {
            if (sourceId == locationId && targetId == locationId) return "interno";
            if (sourceId == locationId) return "salida";
            if (targetId == locationId) return "entrada";
            return "indefinido";
        }

        private static decimal ToDecimal(object value)
        {
            if (value == DBNull.Value || value == null) return 0m;
            return Convert.ToDecimal(value);
        }

        private static decimal? ToNullableDecimal(object value)
        {
            if (value == DBNull.Value || value == null) return null;
            return Convert.ToDecimal(value);
        }
    }
}
