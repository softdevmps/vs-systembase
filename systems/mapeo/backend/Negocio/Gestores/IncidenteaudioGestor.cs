using Backend.Data;
using Backend.Models.Incidenteaudio;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class IncidenteaudioGestor
    {
        public static List<IncidenteaudioResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [IncidenteId], [FilePath], [Format], [DurationSec], [Hash], [CreatedAt] FROM [sys_mapeo].[IncidenteAudio]");
            sql.Append(" WHERE ISNULL([IsDeleted], 0) = 0");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<IncidenteaudioResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static IncidenteaudioResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [IncidenteId], [FilePath], [Format], [DurationSec], [Hash], [CreatedAt] FROM [sys_mapeo].[IncidenteAudio] WHERE [Id] = @id AND ISNULL([IsDeleted], 0) = 0";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(IncidenteaudioCreateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Filepath != null && request.Filepath.Length > 500) return (false, "MaxLength excedido: FilePath");
            if (request.Format != null && request.Format.Length > 50) return (false, "MaxLength excedido: Format");
            if (request.Hash != null && request.Hash.Length > 64) return (false, "MaxLength excedido: Hash");

            var sql = "INSERT INTO [sys_mapeo].[IncidenteAudio] ([IncidenteId], [FilePath], [Format], [DurationSec], [Hash], [CreatedAt]) VALUES (@IncidenteId, @FilePath, @Format, @DurationSec, @Hash, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", request.Filepath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Format", request.Format ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DurationSec", request.Durationsec ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Hash", request.Hash ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, IncidenteaudioUpdateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Filepath != null && request.Filepath.Length > 500) return (false, "MaxLength excedido: FilePath");
            if (request.Format != null && request.Format.Length > 50) return (false, "MaxLength excedido: Format");
            if (request.Hash != null && request.Hash.Length > 64) return (false, "MaxLength excedido: Hash");
            var sql = "UPDATE [sys_mapeo].[IncidenteAudio] SET [IncidenteId] = @IncidenteId, [FilePath] = @FilePath, [Format] = @Format, [DurationSec] = @DurationSec, [Hash] = @Hash, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", request.Filepath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Format", request.Format ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DurationSec", request.Durationsec ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Hash", request.Hash ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_mapeo].[IncidenteAudio] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public static bool EliminarSoft(int id)
        {
            using var conn = Db.Open();
            var sql = "UPDATE [sys_mapeo].[IncidenteAudio] SET [IsDeleted] = 1, [DeletedAt] = GETUTCDATE() WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static IncidenteaudioResponse MapToResponse(SqlDataReader reader)
        {
            return new IncidenteaudioResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Incidenteid = reader["IncidenteId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["IncidenteId"], typeof(int)),
                Filepath = reader["FilePath"] == DBNull.Value ? null : reader["FilePath"].ToString(),
                Format = reader["Format"] == DBNull.Value ? null : reader["Format"].ToString(),
                Durationsec = reader["DurationSec"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["DurationSec"], typeof(decimal)),
                Hash = reader["Hash"] == DBNull.Value ? null : reader["Hash"].ToString(),
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
