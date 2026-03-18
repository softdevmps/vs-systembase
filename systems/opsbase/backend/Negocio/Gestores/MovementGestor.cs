using Backend.Data;
using Backend.Models.Movement;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class MovementGestor
    {
        public static List<MovementResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt] FROM [sys_opsbase].[Movement]");
            sql.Append(" ORDER BY [Id] ASC");

            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<MovementResponse>();
            while (reader.Read())
                list.Add(MapToResponse(reader));

            return list;
        }

        public static MovementResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            const string sql = "SELECT [Id], [MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt] FROM [sys_opsbase].[Movement] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(MovementCreateRequest request, string? actor = null)
        {
            var movementType = MovementOpsHelper.NormalizeMovementType(request.Movementtype);
            var status = MovementOpsHelper.NormalizeStatus(request.Status);
            var createdBy = string.IsNullOrWhiteSpace(request.Createdby) ? actor : request.Createdby?.Trim();
            var operationAt = request.Operationat == default ? DateTime.UtcNow : request.Operationat;
            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var validation = MovementOpsHelper.ValidateMovement(
                    conn,
                    tx,
                    movementType,
                    status,
                    request.Sourcelocationid,
                    request.Targetlocationid
                );

                if (!validation.Ok)
                {
                    tx.Rollback();
                    return validation;
                }

                const string sql = @"INSERT INTO [sys_opsbase].[Movement]
                                     ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt])
                                     VALUES
                                     (@MovementType, @Status, @SourceLocationId, @TargetLocationId, @ReferenceNo, @Notes, @OperationAt, @CreatedBy, @CreatedAt);
                                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@MovementType", movementType);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@SourceLocationId", request.Sourcelocationid ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ReferenceNo", request.Referenceno ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OperationAt", operationAt);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", createdAt);

                var movementId = Convert.ToInt32(cmd.ExecuteScalar());

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "movement.create",
                    entityName: "Movement",
                    entityId: movementId,
                    result: "ok",
                    message: "Movimiento creado.",
                    actor: actor ?? createdBy,
                    payload: new
                    {
                        movementType,
                        status,
                        request.Sourcelocationid,
                        request.Targetlocationid,
                        request.Referenceno
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al crear movimiento: {ex.Message}");
            }
        }

        public static (bool Ok, string? Error) Editar(int id, MovementUpdateRequest request, string? actor = null)
        {
            var movementType = MovementOpsHelper.NormalizeMovementType(request.Movementtype);
            var status = MovementOpsHelper.NormalizeStatus(request.Status);
            var createdBy = string.IsNullOrWhiteSpace(request.Createdby) ? actor : request.Createdby?.Trim();
            var operationAt = request.Operationat == default ? DateTime.UtcNow : request.Operationat;
            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var current = MovementOpsHelper.GetMovementHeader(conn, tx, id, forUpdate: true);
                if (current == null)
                {
                    tx.Rollback();
                    return (false, "No encontrado");
                }

                if (MovementOpsHelper.IsCancelledStatus(current.Status))
                {
                    tx.Rollback();
                    return (false, "Movimiento anulado: no se permite editar.");
                }

                var validation = MovementOpsHelper.ValidateMovement(
                    conn,
                    tx,
                    movementType,
                    status,
                    request.Sourcelocationid,
                    request.Targetlocationid
                );

                if (!validation.Ok)
                {
                    tx.Rollback();
                    return validation;
                }

                var hasLines = MovementOpsHelper.HasMovementLines(conn, tx, id);
                var changedCriticalFields =
                    !string.Equals(current.MovementType, movementType, StringComparison.OrdinalIgnoreCase)
                    || current.SourceLocationId != request.Sourcelocationid
                    || current.TargetLocationId != request.Targetlocationid;

                if (hasLines && changedCriticalFields)
                {
                    tx.Rollback();
                    return (false, "No se puede cambiar tipo/ubicaciones cuando el movimiento ya tiene lineas.");
                }

                if (MovementOpsHelper.IsConfirmedStatus(current.Status) && !MovementOpsHelper.IsConfirmedStatus(status))
                {
                    tx.Rollback();
                    return (false, "No se permite revertir un movimiento confirmado.");
                }

                const string sqlUpdate = @"UPDATE [sys_opsbase].[Movement]
                                           SET [MovementType] = @MovementType,
                                               [Status] = @Status,
                                               [SourceLocationId] = @SourceLocationId,
                                               [TargetLocationId] = @TargetLocationId,
                                               [ReferenceNo] = @ReferenceNo,
                                               [Notes] = @Notes,
                                               [OperationAt] = @OperationAt,
                                               [CreatedBy] = @CreatedBy,
                                               [CreatedAt] = @CreatedAt
                                           WHERE [Id] = @id;";

                using (var cmd = new SqlCommand(sqlUpdate, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementType", movementType);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@SourceLocationId", request.Sourcelocationid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReferenceNo", request.Referenceno ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OperationAt", operationAt);
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    cmd.Parameters.AddWithValue("@id", id);

                    var rows = cmd.ExecuteNonQuery();
                    if (rows <= 0)
                    {
                        tx.Rollback();
                        return (false, "No encontrado");
                    }
                }

                var lineCountApplied = 0;
                if (!MovementOpsHelper.IsConfirmedStatus(current.Status) && MovementOpsHelper.IsConfirmedStatus(status))
                {
                    var lines = MovementOpsHelper.GetMovementLines(conn, tx, id);
                    foreach (var line in lines)
                    {
                        var apply = MovementOpsHelper.ApplyStockForLine(
                            conn,
                            tx,
                            movementType,
                            request.Sourcelocationid,
                            request.Targetlocationid,
                            line.ResourceInstanceId,
                            line.Quantity
                        );

                        if (!apply.Ok)
                        {
                            tx.Rollback();
                            return apply;
                        }

                        lineCountApplied++;
                    }
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "movement.update",
                    entityName: "Movement",
                    entityId: id,
                    result: "ok",
                    message: "Movimiento actualizado.",
                    actor: actor ?? createdBy ?? current.CreatedBy,
                    payload: new
                    {
                        previousStatus = current.Status,
                        newStatus = status,
                        movementType,
                        request.Sourcelocationid,
                        request.Targetlocationid,
                        appliedLines = lineCountApplied
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al editar movimiento: {ex.Message}");
            }
        }

        public static (bool Ok, string? Error) Eliminar(int id, string? actor = null)
        {
            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var current = MovementOpsHelper.GetMovementHeader(conn, tx, id, forUpdate: true);
                if (current == null)
                {
                    tx.Rollback();
                    return (false, "No encontrado");
                }

                var hasLines = MovementOpsHelper.HasMovementLines(conn, tx, id);
                if (hasLines && MovementOpsHelper.IsConfirmedStatus(current.Status))
                {
                    tx.Rollback();
                    return (false, "No se puede eliminar un movimiento confirmado con lineas aplicadas.");
                }

                if (hasLines)
                {
                    using var cmdDeleteLines = new SqlCommand(
                        "DELETE FROM [sys_opsbase].[MovementLine] WHERE [MovementId] = @movementId;",
                        conn,
                        tx);
                    cmdDeleteLines.Parameters.AddWithValue("@movementId", id);
                    cmdDeleteLines.ExecuteNonQuery();
                }

                using var cmdDelete = new SqlCommand(
                    "DELETE FROM [sys_opsbase].[Movement] WHERE [Id] = @id;",
                    conn,
                    tx);
                cmdDelete.Parameters.AddWithValue("@id", id);

                var rows = cmdDelete.ExecuteNonQuery();
                if (rows <= 0)
                {
                    tx.Rollback();
                    return (false, "No encontrado");
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "movement.delete",
                    entityName: "Movement",
                    entityId: id,
                    result: "ok",
                    message: "Movimiento eliminado.",
                    actor: actor ?? current.CreatedBy,
                    payload: new
                    {
                        current.MovementType,
                        current.Status,
                        current.SourceLocationId,
                        current.TargetLocationId,
                        deletedLines = hasLines
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al eliminar movimiento: {ex.Message}");
            }
        }

        private static MovementResponse MapToResponse(SqlDataReader reader)
        {
            return new MovementResponse
            {
                Id = reader["Id"] == DBNull.Value ? default : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Movementtype = reader["MovementType"] == DBNull.Value ? null : reader["MovementType"].ToString(),
                Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                Sourcelocationid = reader["SourceLocationId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["SourceLocationId"], typeof(int)),
                Targetlocationid = reader["TargetLocationId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["TargetLocationId"], typeof(int)),
                Referenceno = reader["ReferenceNo"] == DBNull.Value ? null : reader["ReferenceNo"].ToString(),
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                Operationat = reader["OperationAt"] == DBNull.Value ? default : (DateTime)Convert.ChangeType(reader["OperationAt"], typeof(DateTime)),
                Createdby = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
            };
        }
    }
}
