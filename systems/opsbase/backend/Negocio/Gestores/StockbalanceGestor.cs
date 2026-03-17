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
            if (ExistsByValue(conn, "sys_opsbase", "StockBalance", "ResourceInstanceId", request.Resourceinstanceid, null, null)) return (false, "Valor duplicado en ResourceInstanceId");
            if (!ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Locationid, null, null)) return (false, "Location inexistente (LocationId)");
            if (ExistsByValue(conn, "sys_opsbase", "StockBalance", "LocationId", request.Locationid, null, null)) return (false, "Valor duplicado en LocationId");

            var sql = "INSERT INTO [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible], [CreatedAt], [UpdatedAt]) VALUES (@ResourceInstanceId, @LocationId, @StockReal, @StockReservado, @StockDisponible, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@LocationId", request.Locationid);
            cmd.Parameters.AddWithValue("@StockReal", request.Stockreal);
            cmd.Parameters.AddWithValue("@StockReservado", request.Stockreservado);
            cmd.Parameters.AddWithValue("@StockDisponible", request.Stockdisponible);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, StockbalanceUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (ExistsByValue(conn, "sys_opsbase", "StockBalance", "ResourceInstanceId", request.Resourceinstanceid, "Id", id)) return (false, "Valor duplicado en ResourceInstanceId");
            if (!ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Locationid, null, null)) return (false, "Location inexistente (LocationId)");
            if (ExistsByValue(conn, "sys_opsbase", "StockBalance", "LocationId", request.Locationid, "Id", id)) return (false, "Valor duplicado en LocationId");
            var sql = "UPDATE [sys_opsbase].[StockBalance] SET [ResourceInstanceId] = @ResourceInstanceId, [LocationId] = @LocationId, [StockReal] = @StockReal, [StockReservado] = @StockReservado, [StockDisponible] = @StockDisponible, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@LocationId", request.Locationid);
            cmd.Parameters.AddWithValue("@StockReal", request.Stockreal);
            cmd.Parameters.AddWithValue("@StockReservado", request.Stockreservado);
            cmd.Parameters.AddWithValue("@StockDisponible", request.Stockdisponible);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat);
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
    }
}
