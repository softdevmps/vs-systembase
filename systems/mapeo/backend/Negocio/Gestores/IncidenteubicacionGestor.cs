using Backend.Data;
using Backend.Models.Incidenteubicacion;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class IncidenteubicacionGestor
    {
        public static List<IncidenteubicacionResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [IncidenteId], [Fuente], [Lat], [Lng], [Precision], [AddressNormalized] FROM [sys_mapeo].[IncidenteUbicacion]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<IncidenteubicacionResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static IncidenteubicacionResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [IncidenteId], [Fuente], [Lat], [Lng], [Precision], [AddressNormalized] FROM [sys_mapeo].[IncidenteUbicacion] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(IncidenteubicacionCreateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Fuente != null && request.Fuente.Length > 50) return (false, "MaxLength excedido: Fuente");
            if (request.Precision != null && request.Precision.Length > 50) return (false, "MaxLength excedido: Precision");
            if (request.Addressnormalized != null && request.Addressnormalized.Length > 255) return (false, "MaxLength excedido: AddressNormalized");

            var sql = "INSERT INTO [sys_mapeo].[IncidenteUbicacion] ([IncidenteId], [Fuente], [Lat], [Lng], [Precision], [AddressNormalized]) VALUES (@IncidenteId, @Fuente, @Lat, @Lng, @Precision, @AddressNormalized);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Fuente", request.Fuente ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lat", request.Lat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lng", request.Lng ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Precision", request.Precision ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AddressNormalized", request.Addressnormalized ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, IncidenteubicacionUpdateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Fuente != null && request.Fuente.Length > 50) return (false, "MaxLength excedido: Fuente");
            if (request.Precision != null && request.Precision.Length > 50) return (false, "MaxLength excedido: Precision");
            if (request.Addressnormalized != null && request.Addressnormalized.Length > 255) return (false, "MaxLength excedido: AddressNormalized");
            var sql = "UPDATE [sys_mapeo].[IncidenteUbicacion] SET [IncidenteId] = @IncidenteId, [Fuente] = @Fuente, [Lat] = @Lat, [Lng] = @Lng, [Precision] = @Precision, [AddressNormalized] = @AddressNormalized WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Fuente", request.Fuente ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lat", request.Lat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lng", request.Lng ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Precision", request.Precision ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AddressNormalized", request.Addressnormalized ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_mapeo].[IncidenteUbicacion] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static IncidenteubicacionResponse MapToResponse(SqlDataReader reader)
        {
            return new IncidenteubicacionResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Incidenteid = reader["IncidenteId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["IncidenteId"], typeof(int)),
                Fuente = reader["Fuente"] == DBNull.Value ? null : reader["Fuente"].ToString(),
                Lat = reader["Lat"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Lat"], typeof(decimal)),
                Lng = reader["Lng"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Lng"], typeof(decimal)),
                Precision = reader["Precision"] == DBNull.Value ? null : reader["Precision"].ToString(),
                Addressnormalized = reader["AddressNormalized"] == DBNull.Value ? null : reader["AddressNormalized"].ToString(),
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
