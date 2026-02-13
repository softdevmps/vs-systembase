using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Pipeline
{
    public sealed class CatalogoHechoItem
    {
        public int Id { get; init; }
        public string Codigo { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string Categoria { get; init; } = "";
        public string Subcategoria { get; init; } = "";
        public string PalabrasClave { get; init; } = "";
    }

    public sealed class PipelineJob
    {
        public int Id { get; init; }
        public int IncidenteId { get; init; }
        public int Attempts { get; init; }
    }

    public sealed class IncidenteAudioInfo
    {
        public int Id { get; init; }
        public int IncidenteId { get; init; }
        public string? FilePath { get; init; }
        public string? Format { get; init; }
        public string? Hash { get; init; }
    }

    public sealed class IncidentePipelineRepository
    {
        public (int IncidenteId, int AudioId, int JobId) CrearIncidenteConAudio(
            string lugarTexto,
            DateTime fechaHora,
            string estado,
            string filePath,
            string format,
            string hash
        )
        {
            using var conn = Db.Open();

            const string sqlIncidente = @"
INSERT INTO [sys_mapeo].[Incidentes]
([FechaHora], [LugarTexto], [Estado], [CreatedAt])
VALUES (@FechaHora, @LugarTexto, @Estado, @CreatedAt);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmdIncidente = new SqlCommand(sqlIncidente, conn);
            cmdIncidente.Parameters.AddWithValue("@FechaHora", fechaHora);
            cmdIncidente.Parameters.AddWithValue("@LugarTexto", lugarTexto);
            cmdIncidente.Parameters.AddWithValue("@Estado", estado);
            cmdIncidente.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            var incidenteId = Convert.ToInt32(cmdIncidente.ExecuteScalar());

            const string sqlAudio = @"
INSERT INTO [sys_mapeo].[IncidenteAudio]
([IncidenteId], [FilePath], [Format], [Hash], [CreatedAt])
VALUES (@IncidenteId, @FilePath, @Format, @Hash, @CreatedAt);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmdAudio = new SqlCommand(sqlAudio, conn);
            cmdAudio.Parameters.AddWithValue("@IncidenteId", incidenteId);
            cmdAudio.Parameters.AddWithValue("@FilePath", filePath);
            cmdAudio.Parameters.AddWithValue("@Format", format);
            cmdAudio.Parameters.AddWithValue("@Hash", hash);
            cmdAudio.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            var audioId = Convert.ToInt32(cmdAudio.ExecuteScalar());

            const string sqlJob = @"
INSERT INTO [sys_mapeo].[IncidenteJobs]
([IncidenteId], [Status], [Step], [Attempts], [LastError], [CreatedAt], [UpdateAt])
VALUES (@IncidenteId, @Status, @Step, @Attempts, @LastError, @CreatedAt, @UpdateAt);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmdJob = new SqlCommand(sqlJob, conn);
            cmdJob.Parameters.AddWithValue("@IncidenteId", incidenteId);
            cmdJob.Parameters.AddWithValue("@Status", "pending");
            cmdJob.Parameters.AddWithValue("@Step", "queued");
            cmdJob.Parameters.AddWithValue("@Attempts", 0);
            cmdJob.Parameters.AddWithValue("@LastError", DBNull.Value);
            cmdJob.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            cmdJob.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            var jobId = Convert.ToInt32(cmdJob.ExecuteScalar());

            return (incidenteId, audioId, jobId);
        }

        public PipelineJob? ObtenerSiguienteJob()
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT TOP 1 [Id], [IncidenteId], [Attempts]
FROM [sys_mapeo].[IncidenteJobs] WITH (UPDLOCK, READPAST)
WHERE [Status] IN ('pending', 'retry')
ORDER BY [Id] ASC;";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new PipelineJob
            {
                Id = Convert.ToInt32(reader["Id"]),
                IncidenteId = Convert.ToInt32(reader["IncidenteId"]),
                Attempts = reader["Attempts"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Attempts"])
            };
        }

        public void MarcarJobProcesando(int jobId, int attempts)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'processing',
    [Step] = 'processing',
    [Attempts] = @Attempts,
    [UpdateAt] = @UpdateAt
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Attempts", attempts);
            cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void MarcarJobError(int jobId, string error)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'error',
    [Step] = 'error',
    [LastError] = @Error,
    [UpdateAt] = @UpdateAt
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Error", error);
            cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void MarcarJobRetry(int jobId, string error)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'retry',
    [Step] = 'retry',
    [LastError] = @Error,
    [UpdateAt] = @UpdateAt
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Error", error);
            cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void MarcarJobFinalizado(int jobId)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'done',
    [Step] = 'done',
    [UpdateAt] = @UpdateAt
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", jobId);
            cmd.ExecuteNonQuery();
        }

        public int ResetStuckProcessingJobs(int minutes)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'retry',
    [Step] = 'timeout',
    [LastError] = 'Timeout processing job',
    [UpdateAt] = @UpdateAt
WHERE [Status] = 'processing'
  AND [UpdateAt] < DATEADD(MINUTE, -@Minutes, GETUTCDATE());";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Minutes", minutes);
            cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow);
            return cmd.ExecuteNonQuery();
        }

        public IncidenteAudioInfo? ObtenerAudioPorIncidente(int incidenteId)
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT TOP 1 [Id], [IncidenteId], [FilePath], [Format], [Hash]
FROM [sys_mapeo].[IncidenteAudio]
WHERE [IncidenteId] = @IncidenteId AND ISNULL([IsDeleted], 0) = 0
ORDER BY [Id] DESC;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", incidenteId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new IncidenteAudioInfo
            {
                Id = Convert.ToInt32(reader["Id"]),
                IncidenteId = Convert.ToInt32(reader["IncidenteId"]),
                FilePath = reader["FilePath"]?.ToString(),
                Format = reader["Format"]?.ToString(),
                Hash = reader["Hash"]?.ToString()
            };
        }

        public List<CatalogoHechoItem> ObtenerCatalogoHechos()
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT [Id], [Codigo], [Nombre], [Categoria], [Subcategoria], [PalabrasClave]
FROM [sys_mapeo].[CatalogoHechos]
WHERE [Activo] = 1
ORDER BY [Id] ASC;";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            var list = new List<CatalogoHechoItem>();
            while (reader.Read())
            {
                list.Add(new CatalogoHechoItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Codigo = reader["Codigo"]?.ToString() ?? "",
                    Nombre = reader["Nombre"]?.ToString() ?? "",
                    Categoria = reader["Categoria"]?.ToString() ?? "",
                    Subcategoria = reader["Subcategoria"]?.ToString() ?? "",
                    PalabrasClave = reader["PalabrasClave"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public void InsertarExtraccion(int incidenteId, string rawText, string jsonExtract, string scoresJson, string modelVersion, string language, decimal? confidence)
        {
            using var conn = Db.Open();
            const string sql = @"
INSERT INTO [sys_mapeo].[IncidenteExtraccion]
([IncidenteId], [RawText], [JsonExtract], [ScoresJson], [ModelVersion], [Language], [Confidence], [CreatedAt])
VALUES (@IncidenteId, @RawText, @JsonExtract, @ScoresJson, @ModelVersion, @Language, @Confidence, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", incidenteId);
            cmd.Parameters.AddWithValue("@RawText", rawText);
            cmd.Parameters.AddWithValue("@JsonExtract", jsonExtract);
            cmd.Parameters.AddWithValue("@ScoresJson", scoresJson);
            cmd.Parameters.AddWithValue("@ModelVersion", modelVersion);
            cmd.Parameters.AddWithValue("@Language", language);
            cmd.Parameters.AddWithValue("@Confidence", confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public void InsertarUbicacion(int incidenteId, decimal lat, decimal lng, string precision, string fuente, string? addressNormalized)
        {
            using var conn = Db.Open();
            const string sql = @"
INSERT INTO [sys_mapeo].[IncidenteUbicacion]
([IncidenteId], [Fuente], [Lat], [Lng], [Precision], [AddressNormalized])
VALUES (@IncidenteId, @Fuente, @Lat, @Lng, @Precision, @AddressNormalized);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", incidenteId);
            cmd.Parameters.AddWithValue("@Fuente", fuente);
            cmd.Parameters.AddWithValue("@Lat", lat);
            cmd.Parameters.AddWithValue("@Lng", lng);
            cmd.Parameters.AddWithValue("@Precision", precision);
            cmd.Parameters.AddWithValue("@AddressNormalized", addressNormalized ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarIncidente(
            int incidenteId,
            DateTime? fechaHora,
            string lugarTexto,
            string? lugarNormalizado,
            int? tipoHechoId,
            string? descripcion,
            decimal? lat,
            decimal? lng,
            decimal? confidence,
            string estado
        )
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[Incidentes]
SET [FechaHora] = @FechaHora,
    [LugarTexto] = @LugarTexto,
    [LugarNormalizado] = @LugarNormalizado,
    [TipoHechoId] = @TipoHechoId,
    [Descripcion] = @Descripcion,
    [Lat] = @Lat,
    [Lng] = @Lng,
    [Confidence] = @Confidence,
    [Estado] = @Estado
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FechaHora", fechaHora ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@LugarTexto", lugarTexto);
            cmd.Parameters.AddWithValue("@LugarNormalizado", lugarNormalizado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoHechoId", tipoHechoId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lat", lat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lng", lng ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidence", confidence ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", estado);
            cmd.Parameters.AddWithValue("@Id", incidenteId);
            cmd.ExecuteNonQuery();
        }

        public void MarcarAudioEliminado(int audioId)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteAudio]
SET [IsDeleted] = 1,
    [DeletedAt] = @DeletedAt
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeletedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", audioId);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarAudioArchivo(int audioId, string filePath, string format, string hash)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteAudio]
SET [FilePath] = @FilePath,
    [Format] = @Format,
    [Hash] = @Hash
WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FilePath", filePath);
            cmd.Parameters.AddWithValue("@Format", format);
            cmd.Parameters.AddWithValue("@Hash", hash);
            cmd.Parameters.AddWithValue("@Id", audioId);
            cmd.ExecuteNonQuery();
        }
    }
}
