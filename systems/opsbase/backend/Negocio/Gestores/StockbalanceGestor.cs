using Backend.Data;
using Backend.Models.Stockbalance;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class StockbalanceGestor
    {
        public static List<StockbalanceResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[StockBalance]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<StockbalanceResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static StockbalanceResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[StockBalance] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(StockbalanceCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (!ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Locationid, null, null)) return (false, "Location inexistente (LocationId)");
            if (ExistsByPair(conn, request.Resourceinstanceid, request.Locationid, null)) return (false, "Ya existe un StockBalance para ese recurso y ubicacion.");
            if (request.Stockreal < 0) return (false, "StockReal no puede ser negativo.");
            if (request.Stockreservado < 0) return (false, "StockReservado no puede ser negativo.");
            if (request.Stockreservado > request.Stockreal) return (false, "StockReservado no puede ser mayor a StockReal.");

            var stockDisponible = request.Stockreal - request.Stockreservado;
            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
            var updatedAt = request.Updatedat == default ? DateTime.UtcNow : request.Updatedat;

            var sql = "INSERT INTO [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible], [CreatedAt], [UpdatedAt]) VALUES (@ResourceInstanceId, @LocationId, @StockReal, @StockReservado, @StockDisponible, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@LocationId", request.Locationid);
            cmd.Parameters.AddWithValue("@StockReal", request.Stockreal);
            cmd.Parameters.AddWithValue("@StockReservado", request.Stockreservado);
            cmd.Parameters.AddWithValue("@StockDisponible", stockDisponible);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", updatedAt);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, StockbalanceUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (!ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Locationid, null, null)) return (false, "Location inexistente (LocationId)");
            if (ExistsByPair(conn, request.Resourceinstanceid, request.Locationid, id)) return (false, "Ya existe un StockBalance para ese recurso y ubicacion.");
            if (request.Stockreal < 0) return (false, "StockReal no puede ser negativo.");
            if (request.Stockreservado < 0) return (false, "StockReservado no puede ser negativo.");
            if (request.Stockreservado > request.Stockreal) return (false, "StockReservado no puede ser mayor a StockReal.");

            var stockDisponible = request.Stockreal - request.Stockreservado;
            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
            var updatedAt = request.Updatedat == default ? DateTime.UtcNow : request.Updatedat;

            var sql = "UPDATE [sys_opsbase].[StockBalance] SET [ResourceInstanceId] = @ResourceInstanceId, [LocationId] = @LocationId, [StockReal] = @StockReal, [StockReservado] = @StockReservado, [StockDisponible] = @StockDisponible, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@LocationId", request.Locationid);
            cmd.Parameters.AddWithValue("@StockReal", request.Stockreal);
            cmd.Parameters.AddWithValue("@StockReservado", request.Stockreservado);
            cmd.Parameters.AddWithValue("@StockDisponible", stockDisponible);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", updatedAt);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_opsbase].[StockBalance] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static StockbalanceResponse MapToResponse(SqlDataReader reader)
        {
            return new StockbalanceResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Resourceinstanceid = reader["ResourceInstanceId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ResourceInstanceId"], typeof(int)),
                Locationid = reader["LocationId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["LocationId"], typeof(int)),
                Stockreal = reader["StockReal"] == DBNull.Value ? default(decimal) : (decimal)Convert.ChangeType(reader["StockReal"], typeof(decimal)),
                Stockreservado = reader["StockReservado"] == DBNull.Value ? default(decimal) : (decimal)Convert.ChangeType(reader["StockReservado"], typeof(decimal)),
                Stockdisponible = reader["StockDisponible"] == DBNull.Value ? default(decimal) : (decimal)Convert.ChangeType(reader["StockDisponible"], typeof(decimal)),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
                Updatedat = reader["UpdatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["UpdatedAt"], typeof(DateTime)),
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

        private static bool ExistsByPair(SqlConnection conn, int resourceInstanceId, int locationId, int? excludeId)
        {
            var sql = "SELECT COUNT(1) FROM [sys_opsbase].[StockBalance] WHERE [ResourceInstanceId] = @resourceInstanceId AND [LocationId] = @locationId";
            if (excludeId != null)
                sql += " AND [Id] <> @excludeId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@resourceInstanceId", resourceInstanceId);
            cmd.Parameters.AddWithValue("@locationId", locationId);
            if (excludeId != null)
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
