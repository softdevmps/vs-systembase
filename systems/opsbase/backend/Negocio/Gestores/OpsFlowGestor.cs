using Backend.Data;
using Backend.Models.OpsFlow;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class OpsFlowGestor
    {
        public static (bool Ok, string? Error, RecepcionCreateResponse? Data) CrearRecepcion(
            RecepcionCreateRequest request,
            string? actor = null)
        {
            if (request.Quantity <= 0)
                return (false, "La cantidad debe ser mayor a cero.", null);

            var status = request.Confirmar ? "confirmado" : "borrador";
            var operationAt = request.Operationat ?? DateTime.UtcNow;
            var createdAt = DateTime.UtcNow;
            var referenceNo = string.IsNullOrWhiteSpace(request.Referenceno)
                ? BuildReferenceNo("ING", operationAt)
                : request.Referenceno.Trim();

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var movementType = "ingreso";
                var validation = MovementOpsHelper.ValidateMovement(
                    conn,
                    tx,
                    movementType,
                    status,
                    sourceLocationId: null,
                    targetLocationId: request.Targetlocationid
                );

                if (!validation.Ok)
                {
                    tx.Rollback();
                    return (false, validation.Error, null);
                }

                if (!MovementOpsHelper.ResourceInstanceIsActive(conn, tx, request.Resourceinstanceid))
                {
                    tx.Rollback();
                    return (false, "ResourceInstance inexistente o inactiva (ResourceInstanceId).", null);
                }

                var createdBy = string.IsNullOrWhiteSpace(actor) ? "runtime-ui" : actor.Trim();

                const string sqlMovement = @"INSERT INTO [sys_opsbase].[Movement]
                                             ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt])
                                             VALUES
                                             (@MovementType, @Status, NULL, @TargetLocationId, @ReferenceNo, @Notes, @OperationAt, @CreatedBy, @CreatedAt);
                                             SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int movementId;
                using (var cmd = new SqlCommand(sqlMovement, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementType", movementType);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid);
                    cmd.Parameters.AddWithValue("@ReferenceNo", referenceNo);
                    cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OperationAt", operationAt);
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    movementId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                const string sqlLine = @"INSERT INTO [sys_opsbase].[MovementLine]
                                         ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt])
                                         VALUES
                                         (@MovementId, @ResourceInstanceId, @Quantity, @UnitCost, NULL, NULL, @CreatedAt);
                                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int lineId;
                using (var cmd = new SqlCommand(sqlLine, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementId", movementId);
                    cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
                    cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    lineId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (request.Confirmar)
                {
                    var apply = MovementOpsHelper.ApplyStockForLine(
                        conn,
                        tx,
                        movementType,
                        sourceLocationId: null,
                        targetLocationId: request.Targetlocationid,
                        resourceInstanceId: request.Resourceinstanceid,
                        quantity: request.Quantity
                    );

                    if (!apply.Ok)
                    {
                        tx.Rollback();
                        return (false, apply.Error, null);
                    }
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "ops.recepcion.create",
                    entityName: "Movement",
                    entityId: movementId,
                    result: "ok",
                    message: request.Confirmar
                        ? "Recepcion guiada creada y confirmada."
                        : "Recepcion guiada creada en borrador.",
                    actor: createdBy,
                    payload: new
                    {
                        movementId,
                        lineId,
                        request.Resourceinstanceid,
                        request.Targetlocationid,
                        request.Quantity,
                        request.Unitcost,
                        status,
                        referenceNo
                    });

                tx.Commit();

                return (true, null, new RecepcionCreateResponse
                {
                    Movementid = movementId,
                    Movementlineid = lineId,
                    Status = status,
                    Referenceno = referenceNo,
                    Operationat = operationAt,
                    Quantity = request.Quantity,
                    Resourceinstanceid = request.Resourceinstanceid,
                    Targetlocationid = request.Targetlocationid
                });
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al crear recepcion guiada: {ex.Message}", null);
            }
        }

        public static (bool Ok, string? Error, DespachoCreateResponse? Data) CrearDespacho(
            DespachoCreateRequest request,
            string? actor = null)
        {
            if (request.Quantity <= 0)
                return (false, "La cantidad debe ser mayor a cero.", null);

            var movementType = MovementOpsHelper.NormalizeMovementType(request.Movementtype);
            if (!movementType.Equals("egreso", StringComparison.OrdinalIgnoreCase)
                && !movementType.Equals("transferencia", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Despacho guiado soporta MovementType egreso o transferencia.", null);
            }

            var status = request.Confirmar ? "confirmado" : "borrador";
            var operationAt = request.Operationat ?? DateTime.UtcNow;
            var createdAt = DateTime.UtcNow;
            var referenceNo = string.IsNullOrWhiteSpace(request.Referenceno)
                ? BuildReferenceNo(movementType.Equals("transferencia", StringComparison.OrdinalIgnoreCase) ? "TRA" : "EGR", operationAt)
                : request.Referenceno.Trim();

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var validation = MovementOpsHelper.ValidateMovement(
                    conn,
                    tx,
                    movementType,
                    status,
                    sourceLocationId: request.Sourcelocationid,
                    targetLocationId: request.Targetlocationid
                );

                if (!validation.Ok)
                {
                    tx.Rollback();
                    return (false, validation.Error, null);
                }

                if (!MovementOpsHelper.ResourceInstanceIsActive(conn, tx, request.Resourceinstanceid))
                {
                    tx.Rollback();
                    return (false, "ResourceInstance inexistente o inactiva (ResourceInstanceId).", null);
                }

                var createdBy = string.IsNullOrWhiteSpace(actor) ? "runtime-ui" : actor.Trim();

                const string sqlMovement = @"INSERT INTO [sys_opsbase].[Movement]
                                             ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt])
                                             VALUES
                                             (@MovementType, @Status, @SourceLocationId, @TargetLocationId, @ReferenceNo, @Notes, @OperationAt, @CreatedBy, @CreatedAt);
                                             SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int movementId;
                using (var cmd = new SqlCommand(sqlMovement, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementType", movementType);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@SourceLocationId", request.Sourcelocationid);
                    cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReferenceNo", referenceNo);
                    cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OperationAt", operationAt);
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    movementId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                const string sqlLine = @"INSERT INTO [sys_opsbase].[MovementLine]
                                         ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt])
                                         VALUES
                                         (@MovementId, @ResourceInstanceId, @Quantity, @UnitCost, NULL, NULL, @CreatedAt);
                                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int lineId;
                using (var cmd = new SqlCommand(sqlLine, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MovementId", movementId);
                    cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
                    cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    lineId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (request.Confirmar)
                {
                    var apply = MovementOpsHelper.ApplyStockForLine(
                        conn,
                        tx,
                        movementType,
                        sourceLocationId: request.Sourcelocationid,
                        targetLocationId: request.Targetlocationid,
                        resourceInstanceId: request.Resourceinstanceid,
                        quantity: request.Quantity
                    );

                    if (!apply.Ok)
                    {
                        tx.Rollback();
                        return (false, apply.Error, null);
                    }
                }

                MovementOpsHelper.WriteAudit(
                    conn,
                    tx,
                    operationName: "ops.despacho.create",
                    entityName: "Movement",
                    entityId: movementId,
                    result: "ok",
                    message: request.Confirmar
                        ? "Despacho guiado creado y confirmado."
                        : "Despacho guiado creado en borrador.",
                    actor: createdBy,
                    payload: new
                    {
                        movementId,
                        lineId,
                        movementType,
                        request.Resourceinstanceid,
                        request.Sourcelocationid,
                        request.Targetlocationid,
                        request.Quantity,
                        request.Unitcost,
                        status,
                        referenceNo
                    });

                tx.Commit();

                return (true, null, new DespachoCreateResponse
                {
                    Movementid = movementId,
                    Movementlineid = lineId,
                    Movementtype = movementType,
                    Status = status,
                    Referenceno = referenceNo,
                    Operationat = operationAt,
                    Quantity = request.Quantity,
                    Resourceinstanceid = request.Resourceinstanceid,
                    Sourcelocationid = request.Sourcelocationid,
                    Targetlocationid = request.Targetlocationid
                });
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, $"Error al crear despacho guiado: {ex.Message}", null);
            }
        }

        private static string BuildReferenceNo(string prefix, DateTime operationAt)
        {
            return $"{prefix}-{operationAt:yyyyMMddHHmmssfff}";
        }
    }
}
