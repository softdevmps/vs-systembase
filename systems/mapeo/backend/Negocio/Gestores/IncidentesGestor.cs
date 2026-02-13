using Backend.Data;
using Backend.Models.Incidentes;
using Backend.Utils;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class IncidentesGestor
    {
        public static List<IncidentesResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [FechaHora], [LugarTexto], [LugarNormalizado], [TipoHechoId], [Descripcion], [Lat], [Lng], [Confidence], [Estado], [CreatedAt] FROM [sys_mapeo].[Incidentes]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<IncidentesResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static IncidentesResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [FechaHora], [LugarTexto], [LugarNormalizado], [TipoHechoId], [Descripcion], [Lat], [Lng], [Confidence], [Estado], [CreatedAt] FROM [sys_mapeo].[Incidentes] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(IncidentesCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Lugartexto)) return (false, "Campo requerido: LugarTexto");
            if (request.Lugartexto != null && request.Lugartexto.Length > 255) return (false, "MaxLength excedido: LugarTexto");
            if (request.Lugarnormalizado != null && request.Lugarnormalizado.Length > 255) return (false, "MaxLength excedido: LugarNormalizado");
            if (request.Tipohechoid != null && !ExistsByValue(conn, "sys_mapeo", "CatalogoHechos", "Id", request.Tipohechoid!, null, null)) return (false, "CatalogoHechos inexistente (TipoHechoId)");
            if (request.Descripcion != null && request.Descripcion.Length > 4000) return (false, "MaxLength excedido: Descripcion");
            if (request.Estado != null && request.Estado.Length > 50) return (false, "MaxLength excedido: Estado");

            var sql = "INSERT INTO [sys_mapeo].[Incidentes] ([FechaHora], [LugarTexto], [LugarNormalizado], [TipoHechoId], [Descripcion], [Lat], [Lng], [Confidence], [Estado], [CreatedAt]) VALUES (@FechaHora, @LugarTexto, @LugarNormalizado, @TipoHechoId, @Descripcion, @Lat, @Lng, @Confidence, @Estado, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FechaHora", request.Fechahora);
            cmd.Parameters.AddWithValue("@LugarTexto", request.Lugartexto ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LugarNormalizado", request.Lugarnormalizado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoHechoId", request.Tipohechoid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lat", request.Lat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lng", request.Lng ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidence", request.Confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", request.Estado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, IncidentesUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Lugartexto)) return (false, "Campo requerido: LugarTexto");
            if (request.Lugartexto != null && request.Lugartexto.Length > 255) return (false, "MaxLength excedido: LugarTexto");
            if (request.Lugarnormalizado != null && request.Lugarnormalizado.Length > 255) return (false, "MaxLength excedido: LugarNormalizado");
            if (request.Tipohechoid != null && !ExistsByValue(conn, "sys_mapeo", "CatalogoHechos", "Id", request.Tipohechoid!, null, null)) return (false, "CatalogoHechos inexistente (TipoHechoId)");
            if (request.Descripcion != null && request.Descripcion.Length > 4000) return (false, "MaxLength excedido: Descripcion");
            if (request.Estado != null && request.Estado.Length > 50) return (false, "MaxLength excedido: Estado");
            var sql = "UPDATE [sys_mapeo].[Incidentes] SET [FechaHora] = @FechaHora, [LugarTexto] = @LugarTexto, [LugarNormalizado] = @LugarNormalizado, [TipoHechoId] = @TipoHechoId, [Descripcion] = @Descripcion, [Lat] = @Lat, [Lng] = @Lng, [Confidence] = @Confidence, [Estado] = @Estado, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FechaHora", request.Fechahora);
            cmd.Parameters.AddWithValue("@LugarTexto", request.Lugartexto ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LugarNormalizado", request.Lugarnormalizado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoHechoId", request.Tipohechoid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lat", request.Lat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lng", request.Lng ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidence", request.Confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", request.Estado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();
            var audioPaths = new List<string?>();

            try
            {
                using (var cmdPaths = new SqlCommand("SELECT [FilePath] FROM [sys_mapeo].[IncidenteAudio] WHERE [IncidenteId] = @id", conn, tx))
                {
                    cmdPaths.Parameters.AddWithValue("@id", id);
                    using var reader = cmdPaths.ExecuteReader();
                    while (reader.Read())
                    {
                        audioPaths.Add(reader["FilePath"] == DBNull.Value ? null : reader["FilePath"].ToString());
                    }
                }

                using (var cmd = new SqlCommand("DELETE FROM [sys_mapeo].[IncidenteExtraccion] WHERE [IncidenteId] = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM [sys_mapeo].[IncidenteUbicacion] WHERE [IncidenteId] = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM [sys_mapeo].[IncidenteJobs] WHERE [IncidenteId] = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM [sys_mapeo].[IncidenteAudio] WHERE [IncidenteId] = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                int rows;
                using (var cmd = new SqlCommand("DELETE FROM [sys_mapeo].[Incidentes] WHERE [Id] = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    rows = cmd.ExecuteNonQuery();
                }

                tx.Commit();

                foreach (var path in audioPaths)
                {
                    if (!string.IsNullOrWhiteSpace(path))
                        AudioStorage.Delete(path);
                }

                return rows > 0;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static IncidentesResponse MapToResponse(SqlDataReader reader)
        {
            return new IncidentesResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Fechahora = reader["FechaHora"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["FechaHora"], typeof(DateTime)),
                Lugartexto = reader["LugarTexto"] == DBNull.Value ? null : reader["LugarTexto"].ToString(),
                Lugarnormalizado = reader["LugarNormalizado"] == DBNull.Value ? null : reader["LugarNormalizado"].ToString(),
                Tipohechoid = reader["TipoHechoId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["TipoHechoId"], typeof(int)),
                Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"].ToString(),
                Lat = reader["Lat"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Lat"], typeof(decimal)),
                Lng = reader["Lng"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Lng"], typeof(decimal)),
                Confidence = reader["Confidence"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Confidence"], typeof(decimal)),
                Estado = reader["Estado"] == DBNull.Value ? null : reader["Estado"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
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
