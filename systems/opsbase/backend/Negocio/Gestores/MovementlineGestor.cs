using Backend.Data;
using Backend.Models.Movementline;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class MovementlineGestor
    {
        public static List<MovementlineResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt] FROM [sys_opsbase].[MovementLine]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<MovementlineResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static MovementlineResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt] FROM [sys_opsbase].[MovementLine] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(MovementlineCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "Movement", "Id", request.Movementid, null, null)) return (false, "Movement inexistente (MovementId)");
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (request.Serie != null && request.Serie.Length > 120) return (false, "MaxLength excedido: Serie");
            if (request.Lote != null && request.Lote.Length > 120) return (false, "MaxLength excedido: Lote");

            var sql = "INSERT INTO [sys_opsbase].[MovementLine] ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote], [CreatedAt]) VALUES (@MovementId, @ResourceInstanceId, @Quantity, @UnitCost, @Serie, @Lote, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovementId", request.Movementid);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
            cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, MovementlineUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "Movement", "Id", request.Movementid, null, null)) return (false, "Movement inexistente (MovementId)");
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (request.Serie != null && request.Serie.Length > 120) return (false, "MaxLength excedido: Serie");
            if (request.Lote != null && request.Lote.Length > 120) return (false, "MaxLength excedido: Lote");
            var sql = "UPDATE [sys_opsbase].[MovementLine] SET [MovementId] = @MovementId, [ResourceInstanceId] = @ResourceInstanceId, [Quantity] = @Quantity, [UnitCost] = @UnitCost, [Serie] = @Serie, [Lote] = @Lote, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovementId", request.Movementid);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
            cmd.Parameters.AddWithValue("@UnitCost", request.Unitcost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_opsbase].[MovementLine] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static MovementlineResponse MapToResponse(SqlDataReader reader)
        {
            return new MovementlineResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Movementid = reader["MovementId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["MovementId"], typeof(int)),
                Resourceinstanceid = reader["ResourceInstanceId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ResourceInstanceId"], typeof(int)),
                Quantity = reader["Quantity"] == DBNull.Value ? default(decimal) : (decimal)Convert.ChangeType(reader["Quantity"], typeof(decimal)),
                Unitcost = reader["UnitCost"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["UnitCost"], typeof(decimal)),
                Serie = reader["Serie"] == DBNull.Value ? null : reader["Serie"].ToString(),
                Lote = reader["Lote"] == DBNull.Value ? null : reader["Lote"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
            };
        }

        private static bool ExistsByValue(SqlConnection conn, string schema, string table, string column, object value, string? idColumn, object? idValue)
        {
            var sql = $"SELECT COUNT(1) FROM [{schema}].[{table}] WHERE [{column}] = @val";
            if (!string.IsNullOrWhiteSpace(idColumn))
                sql += $" AND [{idColumn}] <> @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            if (!string.IsNullOrWhiteSpace(idColumn))
                cmd.Parameters.AddWithValue("@id", idValue!);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
