using Backend.Data;
using Backend.Models.Movementline;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class MovementlineGestor
    {
        private sealed class MovementLineCurrent
        {
            public int Id { get; set; }
            public int MovementId { get; set; }
            public int ResourceInstanceId { get; set; }
            public decimal Quantity { get; set; }
        }

        public static List<MovementlineResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt] FROM [sys_opsbase].[MovementLine]");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<MovementlineResponse>();
            while (reader.Read())
                list.Add(MapToResponse(reader));

            return list;
        }

        public static MovementlineResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            const string sql = "SELECT [Id], [MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt] FROM [sys_opsbase].[MovementLine] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(MovementlineCreateRequest request, string? actor = null)
        {
            if (request.Quantity <= 0)
                return (false, "Quantity debe ser mayor a cero.");

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var movement = MovementOpsHelper.GetMovementHeader(conn, tx, request.Movementid, forUpdate: true);
                if (movement == null)
                {
                    tx.Rollback();
                    return (false, "Movement inexistente (MovementId)");
                }

                if (MovementOpsHelper.IsCancelledStatus(movement.Status))
                {
                    tx.Rollback();
                    return (false, "No se pueden agregar lineas a un movimiento anulado.");
                }

                if (!MovementOpsHelper.ResourceInstanceIsActive(conn, tx, request.Resourceinstanceid))
                {
                    tx.Rollback();
                    return (false, "ResourceInstance inexistente o inactiva (ResourceInstanceId)");
                }

                var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
                const string sql = @"INSERT INTO [sys_opsbase].[MovementLine]
                                     ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt])
                                     VALUES
                                     (@MovementId, @ResourceInstanceId, @Quantity, @UnitCost, @Serie, @Lote, @CreatedAt);
                                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int lineId;
                using (var cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementId", request.Movementid);
                    cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
                    cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    lineId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                var stockApplied = false;
                if (MovementOpsHelper.IsConfirmedStatus(movement.Status))
                {
                    var apply = MovementOpsHelper.ApplyStockForLine(
                        conn,
                        tx,
                        MovementOpsHelper.NormalizeMovementType(movement.MovementType),
                        movement.SourceLocationId,
                        movement.TargetLocationId,
                        request.Resourceinstanceid,
                        request.Quantity
                    );

                    if (!apply.Ok)
                    {
                        tx.Rollback();
                        return apply;
                    }

                    stockApplied = true;
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "movement_line.create",
                    entityName: "MovementLine",
                    entityId: lineId,
                    result: "ok",
                    message: "Linea de movimiento creada.",
                    actor: actor ?? movement.CreatedBy,
                    payload: new
                    {
                        request.Movementid,
                        request.Resourceinstanceid,
                        request.Quantity,
                        movementType = movement.MovementType,
                        movementStatus = movement.Status,
                        stockApplied
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al crear linea de movimiento: {ex.Message}");
            }
        }

        public static (bool Ok, string? Error) Editar(int id, MovementlineUpdateRequest request, string? actor = null)
        {
            if (request.Quantity <= 0)
                return (false, "Quantity debe ser mayor a cero.");

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var currentLine = GetLineForUpdate(conn, tx, id);
                if (currentLine == null)
                {
                    tx.Rollback();
                    return (false, "No encontrado");
                }

                if (request.Movementid != currentLine.MovementId)
                {
                    tx.Rollback();
                    return (false, "No se permite cambiar MovementId en una linea existente.");
                }

                var movement = MovementOpsHelper.GetMovementHeader(conn, tx, currentLine.MovementId, forUpdate: true);
                if (movement == null)
                {
                    tx.Rollback();
                    return (false, "Movement inexistente (MovementId)");
                }

                if (MovementOpsHelper.IsConfirmedStatus(movement.Status))
                {
                    tx.Rollback();
                    return (false, "No se permite editar lineas de movimientos confirmados.");
                }

                if (MovementOpsHelper.IsCancelledStatus(movement.Status))
                {
                    tx.Rollback();
                    return (false, "No se permite editar lineas de movimientos anulados.");
                }

                if (!MovementOpsHelper.ResourceInstanceIsActive(conn, tx, request.Resourceinstanceid))
                {
                    tx.Rollback();
                    return (false, "ResourceInstance inexistente o inactiva (ResourceInstanceId)");
                }

                var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
                const string sql = @"UPDATE [sys_opsbase].[MovementLine]
                                     SET [ResourceInstanceId] = @ResourceInstanceId,
                                         [Quantity] = @Quantity,
                                         [UnitCost] = @UnitCost,
                                         [Serie] = @Serie,
                                         [Lote] = @Lote,
                                         [CreatedAt] = @CreatedAt
                                     WHERE [Id] = @id;";

                using (var cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
                    cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    cmd.Parameters.AddWithValue("@id", id);

                    var rows = cmd.ExecuteNonQuery();
                    if (rows <= 0)
                    {
                        tx.Rollback();
                        return (false, "No encontrado");
                    }
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "movement_line.update",
                    entityName: "MovementLine",
                    entityId: id,
                    result: "ok",
                    message: "Linea de movimiento actualizada.",
                    actor: actor ?? movement.CreatedBy,
                    payload: new
                    {
                        request.Movementid,
                        request.Resourceinstanceid,
                        request.Quantity
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al editar linea de movimiento: {ex.Message}");
            }
        }

        public static (bool Ok, string? Error) Eliminar(int id, string? actor = null)
        {
            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var currentLine = GetLineForUpdate(conn, tx, id);
                if (currentLine == null)
                {
                    tx.Rollback();
                    return (false, "No encontrado");
                }

                var movement = MovementOpsHelper.GetMovementHeader(conn, tx, currentLine.MovementId, forUpdate: true);
                if (movement == null)
                {
                    tx.Rollback();
                    return (false, "Movement inexistente");
                }

                if (MovementOpsHelper.IsConfirmedStatus(movement.Status))
                {
                    tx.Rollback();
                    return (false, "No se permite eliminar lineas de movimientos confirmados.");
                }

                if (MovementOpsHelper.IsCancelledStatus(movement.Status))
                {
                    tx.Rollback();
                    return (false, "No se permite eliminar lineas de movimientos anulados.");
                }

                using var cmdDelete = new SqlCommand("DELETE FROM [sys_opsbase].[MovementLine] WHERE [Id] = @id;", conn, tx);
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
                    operationName: "movement_line.delete",
                    entityName: "MovementLine",
                    entityId: id,
                    result: "ok",
                    message: "Linea de movimiento eliminada.",
                    actor: actor ?? movement.CreatedBy,
                    payload: new
                    {
                        movementId = currentLine.MovementId,
                        currentLine.ResourceInstanceId,
                        currentLine.Quantity
                    });

                tx.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al eliminar linea de movimiento: {ex.Message}");
            }
        }

        private static MovementLineCurrent? GetLineForUpdate(SqlConnection conn, SqlTransaction tx, int id)
        {
            const string sql = @"SELECT TOP 1 [Id], [MovementId], [ResourceInstanceId], [Quantity]
                                 FROM [sys_opsbase].[MovementLine] WITH (UPDLOCK, ROWLOCK)
                                 WHERE [Id] = @id;";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new MovementLineCurrent
            {
                Id = Convert.ToInt32(reader["Id"]),
                MovementId = Convert.ToInt32(reader["MovementId"]),
                ResourceInstanceId = Convert.ToInt32(reader["ResourceInstanceId"]),
                Quantity = Convert.ToDecimal(reader["Quantity"])
            };
        }

        private static MovementlineResponse MapToResponse(SqlDataReader reader)
        {
            return new MovementlineResponse
            {
                Id = reader["Id"] == DBNull.Value ? default : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Movementid = reader["MovementId"] == DBNull.Value ? default : (int)Convert.ChangeType(reader["MovementId"], typeof(int)),
                Resourceinstanceid = reader["ResourceInstanceId"] == DBNull.Value ? default : (int)Convert.ChangeType(reader["ResourceInstanceId"], typeof(int)),
                Quantity = reader["Quantity"] == DBNull.Value ? default : (decimal)Convert.ChangeType(reader["Quantity"], typeof(decimal)),
                Unitcost = reader["UnitCost"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["UnitCost"], typeof(decimal)),
                Serie = reader["Serie"] == DBNull.Value ? null : reader["Serie"].ToString(),
                Lote = reader["Lote"] == DBNull.Value ? null : reader["Lote"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
            };
        }
    }
}
