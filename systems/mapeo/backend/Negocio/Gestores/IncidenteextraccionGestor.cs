using Backend.Data;
using Backend.Models.Incidenteextraccion;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class IncidenteextraccionGestor
    {
        public static List<IncidenteextraccionResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [IncidenteId], [RawText], [JsonExtract], [ScoresJson], [ModelVersion], [Language], [Confidence], [CreatedAt] FROM [sys_mapeo].[IncidenteExtraccion]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<IncidenteextraccionResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static IncidenteextraccionResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [IncidenteId], [RawText], [JsonExtract], [ScoresJson], [ModelVersion], [Language], [Confidence], [CreatedAt] FROM [sys_mapeo].[IncidenteExtraccion] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(IncidenteextraccionCreateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Rawtext != null && request.Rawtext.Length > 4000) return (false, "MaxLength excedido: RawText");
            if (request.Jsonextract != null && request.Jsonextract.Length > 4000) return (false, "MaxLength excedido: JsonExtract");
            if (request.Scoresjson != null && request.Scoresjson.Length > 2000) return (false, "MaxLength excedido: ScoresJson");
            if (request.Modelversion != null && request.Modelversion.Length > 50) return (false, "MaxLength excedido: ModelVersion");
            if (request.Language != null && request.Language.Length > 10) return (false, "MaxLength excedido: Language");

            var sql = "INSERT INTO [sys_mapeo].[IncidenteExtraccion] ([IncidenteId], [RawText], [JsonExtract], [ScoresJson], [ModelVersion], [Language], [Confidence], [CreatedAt]) VALUES (@IncidenteId, @RawText, @JsonExtract, @ScoresJson, @ModelVersion, @Language, @Confidence, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RawText", request.Rawtext ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@JsonExtract", request.Jsonextract ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ScoresJson", request.Scoresjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ModelVersion", request.Modelversion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Language", request.Language ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidence", request.Confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, IncidenteextraccionUpdateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Rawtext != null && request.Rawtext.Length > 4000) return (false, "MaxLength excedido: RawText");
            if (request.Jsonextract != null && request.Jsonextract.Length > 4000) return (false, "MaxLength excedido: JsonExtract");
            if (request.Scoresjson != null && request.Scoresjson.Length > 2000) return (false, "MaxLength excedido: ScoresJson");
            if (request.Modelversion != null && request.Modelversion.Length > 50) return (false, "MaxLength excedido: ModelVersion");
            if (request.Language != null && request.Language.Length > 10) return (false, "MaxLength excedido: Language");
            var sql = "UPDATE [sys_mapeo].[IncidenteExtraccion] SET [IncidenteId] = @IncidenteId, [RawText] = @RawText, [JsonExtract] = @JsonExtract, [ScoresJson] = @ScoresJson, [ModelVersion] = @ModelVersion, [Language] = @Language, [Confidence] = @Confidence, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RawText", request.Rawtext ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@JsonExtract", request.Jsonextract ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ScoresJson", request.Scoresjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ModelVersion", request.Modelversion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Language", request.Language ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidence", request.Confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_mapeo].[IncidenteExtraccion] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static IncidenteextraccionResponse MapToResponse(SqlDataReader reader)
        {
            return new IncidenteextraccionResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Incidenteid = reader["IncidenteId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["IncidenteId"], typeof(int)),
                Rawtext = reader["RawText"] == DBNull.Value ? null : reader["RawText"].ToString(),
                Jsonextract = reader["JsonExtract"] == DBNull.Value ? null : reader["JsonExtract"].ToString(),
                Scoresjson = reader["ScoresJson"] == DBNull.Value ? null : reader["ScoresJson"].ToString(),
                Modelversion = reader["ModelVersion"] == DBNull.Value ? null : reader["ModelVersion"].ToString(),
                Language = reader["Language"] == DBNull.Value ? null : reader["Language"].ToString(),
                Confidence = reader["Confidence"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["Confidence"], typeof(decimal)),
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
