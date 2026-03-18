using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    internal static class MovementOpsHelper
    {
        private const string Schema = "sys_opsbase";

        private static readonly HashSet<string> AllowedMovementTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "ingreso", "egreso", "transferencia", "ajuste", "reserva", "liberacion"
        };

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "borrador", "confirmado", "anulado"
        };

        internal sealed record MovementHeader(
            int Id,
            string MovementType,
            string Status,
            int? SourceLocationId,
            int? TargetLocationId,
            string? CreatedBy
        );

        internal sealed record MovementLineRow(
            int Id,
            int ResourceInstanceId,
            decimal Quantity
        );

        private sealed class StockBalanceSnapshot
        {
            public int Id { get; set; }
            public int LocationId { get; set; }
            public decimal StockReal { get; set; }
            public decimal StockReservado { get; set; }
        }

        internal static string NormalizeMovementType(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        internal static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? "confirmado" : normalized;
        }

        internal static bool IsConfirmedStatus(string status)
            => status.Equals("confirmado", StringComparison.OrdinalIgnoreCase);

        internal static bool IsCancelledStatus(string status)
            => status.Equals("anulado", StringComparison.OrdinalIgnoreCase);

        internal static bool ResourceInstanceIsActive(SqlConnection conn, SqlTransaction tx, int resourceInstanceId)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM [sys_opsbase].[ResourceInstance]
                                 WHERE [Id] = @id AND [IsActive] = 1;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", resourceInstanceId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        internal static (bool Ok, string? Error) ValidateMovement(
            SqlConnection conn,
            SqlTransaction tx,
            string movementType,
            string status,
            int? sourceLocationId,
            int? targetLocationId)
        {
            if (!AllowedMovementTypes.Contains(movementType))
                return (false, "MovementType invalido. Valores permitidos: ingreso, egreso, transferencia, ajuste, reserva, liberacion.");

            if (!AllowedStatuses.Contains(status))
                return (false, "Status invalido. Valores permitidos: borrador, confirmado, anulado.");

            switch (movementType)
            {
                case "ingreso":
                    if (targetLocationId == null)
                        return (false, "Ingreso requiere TargetLocationId.");
                    break;
                case "egreso":
                    if (sourceLocationId == null)
                        return (false, "Egreso requiere SourceLocationId.");
                    break;
                case "transferencia":
                    if (sourceLocationId == null || targetLocationId == null)
                        return (false, "Transferencia requiere SourceLocationId y TargetLocationId.");
                    if (sourceLocationId == targetLocationId)
                        return (false, "Transferencia requiere ubicaciones distintas.");
                    break;
                case "ajuste":
                    if (sourceLocationId == null && targetLocationId == null)
                        return (false, "Ajuste requiere SourceLocationId o TargetLocationId.");
                    if (sourceLocationId != null && targetLocationId != null)
                        return (false, "Ajuste debe indicar solo una ubicacion (origen o destino).");
                    break;
                case "reserva":
                case "liberacion":
                    if (sourceLocationId == null)
                        return (false, $"{movementType} requiere SourceLocationId.");
                    break;
            }

            if (sourceLocationId != null && !LocationIsActive(conn, tx, sourceLocationId.Value))
                return (false, "SourceLocationId no existe o esta inactiva.");

            if (targetLocationId != null && !LocationIsActive(conn, tx, targetLocationId.Value))
                return (false, "TargetLocationId no existe o esta inactiva.");

            return (true, null);
        }

        internal static MovementHeader? GetMovementHeader(SqlConnection conn, SqlTransaction tx, int movementId, bool forUpdate)
        {
            var hint = forUpdate ? " WITH (UPDLOCK, ROWLOCK)" : string.Empty;
            var sql = $@"SELECT TOP 1 [Id], [MovementType], [Status], [SourceLocationId], [TargetLocationId], [CreatedBy]
                         FROM [{Schema}].[Movement]{hint}
                         WHERE [Id] = @id;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", movementId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new MovementHeader(
                Id: Convert.ToInt32(reader["Id"]),
                MovementType: reader["MovementType"]?.ToString() ?? string.Empty,
                Status: reader["Status"]?.ToString() ?? string.Empty,
                SourceLocationId: reader["SourceLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["SourceLocationId"]),
                TargetLocationId: reader["TargetLocationId"] == DBNull.Value ? null : Convert.ToInt32(reader["TargetLocationId"]),
                CreatedBy: reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"]?.ToString()
            );
        }

        internal static bool HasMovementLines(SqlConnection conn, SqlTransaction tx, int movementId)
        {
            const string sql = "SELECT COUNT(1) FROM [sys_opsbase].[MovementLine] WHERE [MovementId] = @movementId;";
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@movementId", movementId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        internal static List<MovementLineRow> GetMovementLines(SqlConnection conn, SqlTransaction tx, int movementId)
        {
            const string sql = @"SELECT [Id], [ResourceInstanceId], [Quantity]
                                 FROM [sys_opsbase].[MovementLine] WITH (UPDLOCK, ROWLOCK)
                                 WHERE [MovementId] = @movementId
                                 ORDER BY [Id] ASC;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@movementId", movementId);

            using var reader = cmd.ExecuteReader();
            var list = new List<MovementLineRow>();
            while (reader.Read())
            {
                list.Add(new MovementLineRow(
                    Id: Convert.ToInt32(reader["Id"]),
                    ResourceInstanceId: Convert.ToInt32(reader["ResourceInstanceId"]),
                    Quantity: Convert.ToDecimal(reader["Quantity"])
                ));
            }

            return list;
        }

        internal static (bool Ok, string? Error) ApplyStockForLine(
            SqlConnection conn,
            SqlTransaction tx,
            string movementType,
            int? sourceLocationId,
            int? targetLocationId,
            int resourceInstanceId,
            decimal quantity)
        {
            if (quantity <= 0)
                return (false, "Quantity debe ser mayor a cero.");

            if (!ResourceInstanceIsActive(conn, tx, resourceInstanceId))
                return (false, $"ResourceInstanceId {resourceInstanceId} no existe o esta inactiva.");

            return movementType switch
            {
                "ingreso" => targetLocationId == null
                    ? (false, "Ingreso requiere TargetLocationId.")
                    : ApplyRealDelta(conn, tx, resourceInstanceId, targetLocationId.Value, quantity),

                "egreso" => sourceLocationId == null
                    ? (false, "Egreso requiere SourceLocationId.")
                    : ApplyRealDelta(conn, tx, resourceInstanceId, sourceLocationId.Value, -quantity),

                "transferencia" => ApplyTransfer(conn, tx, sourceLocationId, targetLocationId, resourceInstanceId, quantity),

                "ajuste" => ApplyAdjustment(conn, tx, sourceLocationId, targetLocationId, resourceInstanceId, quantity),

                "reserva" => sourceLocationId == null
                    ? (false, "Reserva requiere SourceLocationId.")
                    : ApplyReservedDelta(conn, tx, resourceInstanceId, sourceLocationId.Value, quantity),

                "liberacion" => sourceLocationId == null
                    ? (false, "Liberacion requiere SourceLocationId.")
                    : ApplyReservedDelta(conn, tx, resourceInstanceId, sourceLocationId.Value, -quantity),

                _ => (false, $"MovementType invalido: {movementType}.")
            };
        }

        internal static void WriteAudit(
            SqlConnection conn,
            SqlTransaction tx,
            string operationName,
            string entityName,
            int? entityId,
            string result,
            string? message,
            string? actor,
            object? payload,
            Guid? correlationId = null)
        {
            const string sql = @"INSERT INTO [sys_opsbase].[OperationAudit]
                                 ([OperationName], [EntityName], [EntityId], [Result], [Message], [Actor], [CorrelationId], [PayloadJson], [ExecutedAt])
                                 VALUES
                                 (@OperationName, @EntityName, @EntityId, @Result, @Message, @Actor, @CorrelationId, @PayloadJson, @ExecutedAt);";

            var payloadJson = payload == null ? null : JsonSerializer.Serialize(payload);
            var safeCorrelationId = correlationId ?? Guid.NewGuid();

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@OperationName", Limit(operationName, 80) ?? "operation");
            cmd.Parameters.AddWithValue("@EntityName", Limit(entityName, 80) ?? "entity");
            cmd.Parameters.AddWithValue("@EntityId", entityId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Result", Limit(result, 20) ?? "ok");
            cmd.Parameters.AddWithValue("@Message", Limit(message, 500) ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Actor", Limit(actor, 100) ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CorrelationId", safeCorrelationId);
            cmd.Parameters.AddWithValue("@PayloadJson", payloadJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ExecutedAt", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        private static string? Limit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;

            return trimmed[..maxLength];
        }

        private static (bool Ok, string? Error) ApplyTransfer(
            SqlConnection conn,
            SqlTransaction tx,
            int? sourceLocationId,
            int? targetLocationId,
            int resourceInstanceId,
            decimal quantity)
        {
            if (sourceLocationId == null || targetLocationId == null)
                return (false, "Transferencia requiere SourceLocationId y TargetLocationId.");

            if (sourceLocationId == targetLocationId)
                return (false, "Transferencia requiere ubicaciones distintas.");

            var fromResult = ApplyRealDelta(conn, tx, resourceInstanceId, sourceLocationId.Value, -quantity);
            if (!fromResult.Ok)
                return fromResult;

            return ApplyRealDelta(conn, tx, resourceInstanceId, targetLocationId.Value, quantity);
        }

        private static (bool Ok, string? Error) ApplyAdjustment(
            SqlConnection conn,
            SqlTransaction tx,
            int? sourceLocationId,
            int? targetLocationId,
            int resourceInstanceId,
            decimal quantity)
        {
            if (sourceLocationId == null && targetLocationId == null)
                return (false, "Ajuste requiere SourceLocationId o TargetLocationId.");

            if (sourceLocationId != null && targetLocationId != null)
                return (false, "Ajuste debe indicar solo una ubicacion.");

            if (sourceLocationId != null)
                return ApplyRealDelta(conn, tx, resourceInstanceId, sourceLocationId.Value, -quantity);

            return ApplyRealDelta(conn, tx, resourceInstanceId, targetLocationId!.Value, quantity);
        }

        private static (bool Ok, string? Error) ApplyRealDelta(
            SqlConnection conn,
            SqlTransaction tx,
            int resourceInstanceId,
            int locationId,
            decimal delta)
        {
            var balance = EnsureBalance(conn, tx, resourceInstanceId, locationId);
            var nextReal = balance.StockReal + delta;
            if (nextReal < 0)
                return (false, $"Stock insuficiente en ubicacion {locationId} para recurso {resourceInstanceId}.");

            if (balance.StockReservado > nextReal)
                return (false, $"No se puede reducir StockReal por debajo de StockReservado en ubicacion {locationId}.");

            var nextDisponible = nextReal - balance.StockReservado;
            var now = DateTime.UtcNow;

            const string sql = @"UPDATE [sys_opsbase].[StockBalance]
                                 SET [StockReal] = @stockReal,
                                     [StockDisponible] = @stockDisponible,
                                     [UpdatedAt] = @updatedAt
                                 WHERE [Id] = @id;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@stockReal", nextReal);
            cmd.Parameters.AddWithValue("@stockDisponible", nextDisponible);
            cmd.Parameters.AddWithValue("@updatedAt", now);
            cmd.Parameters.AddWithValue("@id", balance.Id);
            cmd.ExecuteNonQuery();

            return (true, null);
        }

        private static (bool Ok, string? Error) ApplyReservedDelta(
            SqlConnection conn,
            SqlTransaction tx,
            int resourceInstanceId,
            int locationId,
            decimal delta)
        {
            var balance = EnsureBalance(conn, tx, resourceInstanceId, locationId);
            var nextReservado = balance.StockReservado + delta;
            if (nextReservado < 0)
                return (false, $"No se puede liberar mas stock del reservado en ubicacion {locationId}.");

            if (nextReservado > balance.StockReal)
                return (false, $"Stock reservado excede stock real en ubicacion {locationId}.");

            var nextDisponible = balance.StockReal - nextReservado;
            var now = DateTime.UtcNow;

            const string sql = @"UPDATE [sys_opsbase].[StockBalance]
                                 SET [StockReservado] = @stockReservado,
                                     [StockDisponible] = @stockDisponible,
                                     [UpdatedAt] = @updatedAt
                                 WHERE [Id] = @id;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@stockReservado", nextReservado);
            cmd.Parameters.AddWithValue("@stockDisponible", nextDisponible);
            cmd.Parameters.AddWithValue("@updatedAt", now);
            cmd.Parameters.AddWithValue("@id", balance.Id);
            cmd.ExecuteNonQuery();

            return (true, null);
        }

        private static StockBalanceSnapshot EnsureBalance(
            SqlConnection conn,
            SqlTransaction tx,
            int resourceInstanceId,
            int locationId)
        {
            var existing = TryGetBalance(conn, tx, resourceInstanceId, locationId);
            if (existing != null)
                return existing;

            var now = DateTime.UtcNow;
            const string sqlInsert = @"INSERT INTO [sys_opsbase].[StockBalance]
                                       ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible], [CreatedAt], [UpdatedAt])
                                       VALUES
                                       (@resourceInstanceId, @locationId, 0, 0, 0, @createdAt, @updatedAt);";

            using var cmd = new SqlCommand(sqlInsert, conn, tx);
            cmd.Parameters.AddWithValue("@resourceInstanceId", resourceInstanceId);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            cmd.Parameters.AddWithValue("@createdAt", now);
            cmd.Parameters.AddWithValue("@updatedAt", now);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                var conflict = TryGetBalanceByResource(conn, tx, resourceInstanceId);
                if (conflict != null && conflict.LocationId != locationId)
                {
                    throw new InvalidOperationException(BuildStockBalanceIndexError(resourceInstanceId, locationId, conflict.LocationId));
                }
            }

            var resolved = TryGetBalance(conn, tx, resourceInstanceId, locationId);
            if (resolved != null)
                return resolved;

            var unresolvedConflict = TryGetBalanceByResource(conn, tx, resourceInstanceId);
            if (unresolvedConflict != null && unresolvedConflict.LocationId != locationId)
            {
                throw new InvalidOperationException(
                    BuildStockBalanceIndexError(resourceInstanceId, locationId, unresolvedConflict.LocationId));
            }

            throw new InvalidOperationException("No se pudo inicializar StockBalance.");
        }

        private static StockBalanceSnapshot? TryGetBalance(
            SqlConnection conn,
            SqlTransaction tx,
            int resourceInstanceId,
            int locationId)
        {
            const string sql = @"SELECT TOP 1 [Id], [LocationId], [StockReal], [StockReservado]
                                 FROM [sys_opsbase].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
                                 WHERE [ResourceInstanceId] = @resourceInstanceId
                                   AND [LocationId] = @locationId;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@resourceInstanceId", resourceInstanceId);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new StockBalanceSnapshot
            {
                Id = Convert.ToInt32(reader["Id"]),
                LocationId = Convert.ToInt32(reader["LocationId"]),
                StockReal = Convert.ToDecimal(reader["StockReal"]),
                StockReservado = Convert.ToDecimal(reader["StockReservado"])
            };
        }

        private static StockBalanceSnapshot? TryGetBalanceByResource(
            SqlConnection conn,
            SqlTransaction tx,
            int resourceInstanceId)
        {
            const string sql = @"SELECT TOP 1 [Id], [LocationId], [StockReal], [StockReservado]
                                 FROM [sys_opsbase].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
                                 WHERE [ResourceInstanceId] = @resourceInstanceId
                                 ORDER BY [Id] DESC;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@resourceInstanceId", resourceInstanceId);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new StockBalanceSnapshot
            {
                Id = Convert.ToInt32(reader["Id"]),
                LocationId = Convert.ToInt32(reader["LocationId"]),
                StockReal = Convert.ToDecimal(reader["StockReal"]),
                StockReservado = Convert.ToDecimal(reader["StockReservado"])
            };
        }

        private static string BuildStockBalanceIndexError(int resourceInstanceId, int requestedLocationId, int existingLocationId)
        {
            return
                $"Configuracion invalida de StockBalance: existe stock para ResourceInstanceId={resourceInstanceId} en LocationId={existingLocationId}, " +
                $"pero no se puede crear otro en LocationId={requestedLocationId}. " +
                "Revisar indice unico y usar clave compuesta (ResourceInstanceId, LocationId).";
        }

        private static bool LocationIsActive(SqlConnection conn, SqlTransaction tx, int locationId)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM [sys_opsbase].[Location]
                                 WHERE [Id] = @id AND [IsActive] = 1;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", locationId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
