using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Utils
{
    public static class DbSchemaMigrator
    {
        public static void Ensure()
        {
            try
            {
                using var conn = Db.Open();
                EnsureColumn(conn, "sys_mapeo", "IncidenteAudio", "IsDeleted", "BIT NOT NULL DEFAULT(0)");
                EnsureColumn(conn, "sys_mapeo", "IncidenteAudio", "DeletedAt", "DATETIME2 NULL");
                EnsureDecimalPrecision(conn, "sys_mapeo", "Incidentes", "Lat", 9, 6);
                EnsureDecimalPrecision(conn, "sys_mapeo", "Incidentes", "Lng", 9, 6);
                EnsureDecimalPrecision(conn, "sys_mapeo", "IncidenteUbicacion", "Lat", 9, 6);
                EnsureDecimalPrecision(conn, "sys_mapeo", "IncidenteUbicacion", "Lng", 9, 6);
                EnsureLocationLearningTables(conn);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbSchema] Error: {ex.Message}");
            }
        }

        private static void EnsureColumn(SqlConnection conn, string schema, string table, string column, string sqlType)
        {
            var checkSql = @"SELECT 1
                             FROM INFORMATION_SCHEMA.COLUMNS
                             WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@schema", schema);
            checkCmd.Parameters.AddWithValue("@table", table);
            checkCmd.Parameters.AddWithValue("@column", column);

            var exists = checkCmd.ExecuteScalar() != null;
            if (exists)
                return;

            var alterSql = $"ALTER TABLE [{schema}].[{table}] ADD [{column}] {sqlType};";
            using var alterCmd = new SqlCommand(alterSql, conn);
            alterCmd.ExecuteNonQuery();
            Console.WriteLine($"[DbSchema] Columna agregada: {schema}.{table}.{column}");
        }

        private static void EnsureDecimalPrecision(SqlConnection conn, string schema, string table, string column, int precision, int scale)
        {
            var checkSql = @"SELECT DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
                             FROM INFORMATION_SCHEMA.COLUMNS
                             WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@schema", schema);
            checkCmd.Parameters.AddWithValue("@table", table);
            checkCmd.Parameters.AddWithValue("@column", column);

            using var reader = checkCmd.ExecuteReader();
            if (!reader.Read())
                return;

            var dataType = reader["DATA_TYPE"]?.ToString() ?? "";
            var precisionObj = reader["NUMERIC_PRECISION"];
            var scaleObj = reader["NUMERIC_SCALE"];
            var isNullable = string.Equals(reader["IS_NULLABLE"]?.ToString(), "YES", StringComparison.OrdinalIgnoreCase);

            var currentPrecision = precisionObj == DBNull.Value ? 0 : Convert.ToInt32(precisionObj);
            var currentScale = scaleObj == DBNull.Value ? 0 : Convert.ToInt32(scaleObj);

            var needsAlter = !string.Equals(dataType, "numeric", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(dataType, "decimal", StringComparison.OrdinalIgnoreCase);

            if (!needsAlter)
            {
                if (currentPrecision < precision || currentScale < scale)
                    needsAlter = true;
            }

            if (!needsAlter)
                return;

            var nullability = isNullable ? "NULL" : "NOT NULL";
            var alterSql = $"ALTER TABLE [{schema}].[{table}] ALTER COLUMN [{column}] DECIMAL({precision},{scale}) {nullability};";
            using var alterCmd = new SqlCommand(alterSql, conn);
            alterCmd.ExecuteNonQuery();
            Console.WriteLine($"[DbSchema] Precision actualizada: {schema}.{table}.{column} -> DECIMAL({precision},{scale})");
        }

        private static void EnsureLocationLearningTables(SqlConnection conn)
        {
            const string createRulesSql = @"
IF OBJECT_ID(N'[sys_mapeo].[LocationNormalizationRules]', N'U') IS NULL
BEGIN
    CREATE TABLE [sys_mapeo].[LocationNormalizationRules]
    (
        [Id] INT IDENTITY(1,1) NOT NULL
            CONSTRAINT [PK_sys_mapeo_LocationNormalizationRules] PRIMARY KEY,
        [FindText] NVARCHAR(200) NOT NULL,
        [ReplaceText] NVARCHAR(200) NOT NULL,
        [Scope] NVARCHAR(30) NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_Scope] DEFAULT('location'),
        [Priority] INT NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_Priority] DEFAULT(100),
        [IsRegex] BIT NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_IsRegex] DEFAULT(0),
        [IsActive] BIT NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_IsActive] DEFAULT(1),
        [Source] NVARCHAR(30) NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_Source] DEFAULT('manual'),
        [HitCount] INT NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_HitCount] DEFAULT(0),
        [LastHitAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationRules_CreatedAt] DEFAULT(SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL
    );
END;";

            const string createFeedbackSql = @"
IF OBJECT_ID(N'[sys_mapeo].[LocationNormalizationFeedback]', N'U') IS NULL
BEGIN
    CREATE TABLE [sys_mapeo].[LocationNormalizationFeedback]
    (
        [Id] INT IDENTITY(1,1) NOT NULL
            CONSTRAINT [PK_sys_mapeo_LocationNormalizationFeedback] PRIMARY KEY,
        [IncidenteId] INT NULL,
        [RawText] NVARCHAR(MAX) NULL,
        [WhisperLocation] NVARCHAR(500) NULL,
        [PredLugarTexto] NVARCHAR(500) NULL,
        [PredLugarNormalizado] NVARCHAR(800) NULL,
        [PredLat] DECIMAL(9,6) NULL,
        [PredLng] DECIMAL(9,6) NULL,
        [CorrectLugarTexto] NVARCHAR(500) NULL,
        [CorrectLugarNormalizado] NVARCHAR(800) NULL,
        [CorrectLat] DECIMAL(9,6) NULL,
        [CorrectLng] DECIMAL(9,6) NULL,
        [Verdict] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationFeedback_Verdict] DEFAULT('pending'),
        [Reviewer] NVARCHAR(100) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL
            CONSTRAINT [DF_sys_mapeo_LocationNormalizationFeedback_CreatedAt] DEFAULT(SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [CK_sys_mapeo_LocationNormalizationFeedback_Verdict]
            CHECK ([Verdict] IN ('pending', 'accepted', 'rejected', 'corrected')),
        CONSTRAINT [FK_sys_mapeo_LocationNormalizationFeedback_IncidenteId]
            FOREIGN KEY ([IncidenteId]) REFERENCES [sys_mapeo].[Incidentes]([Id]) ON DELETE SET NULL
    );
END;";

            const string createRulesIndexesSql = @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_sys_mapeo_LocationNormalizationRules_IsActiveScopePriority'
      AND object_id = OBJECT_ID(N'[sys_mapeo].[LocationNormalizationRules]')
)
BEGIN
    CREATE INDEX [IX_sys_mapeo_LocationNormalizationRules_IsActiveScopePriority]
        ON [sys_mapeo].[LocationNormalizationRules]([IsActive], [Scope], [Priority]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_sys_mapeo_LocationNormalizationRules_FindTextScope_Active'
      AND object_id = OBJECT_ID(N'[sys_mapeo].[LocationNormalizationRules]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_mapeo_LocationNormalizationRules_FindTextScope_Active]
        ON [sys_mapeo].[LocationNormalizationRules]([FindText], [Scope], [IsActive]);
END;";

            const string createFeedbackIndexesSql = @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_sys_mapeo_LocationNormalizationFeedback_IncidenteId'
      AND object_id = OBJECT_ID(N'[sys_mapeo].[LocationNormalizationFeedback]')
)
BEGIN
    CREATE INDEX [IX_sys_mapeo_LocationNormalizationFeedback_IncidenteId]
        ON [sys_mapeo].[LocationNormalizationFeedback]([IncidenteId]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_sys_mapeo_LocationNormalizationFeedback_VerdictCreatedAt'
      AND object_id = OBJECT_ID(N'[sys_mapeo].[LocationNormalizationFeedback]')
)
BEGIN
    CREATE INDEX [IX_sys_mapeo_LocationNormalizationFeedback_VerdictCreatedAt]
        ON [sys_mapeo].[LocationNormalizationFeedback]([Verdict], [CreatedAt]);
END;";

            ExecuteSql(conn, createRulesSql, "Tabla asegurada: sys_mapeo.LocationNormalizationRules");
            ExecuteSql(conn, createFeedbackSql, "Tabla asegurada: sys_mapeo.LocationNormalizationFeedback");
            ExecuteSql(conn, createRulesIndexesSql, "Indices asegurados: sys_mapeo.LocationNormalizationRules");
            ExecuteSql(conn, createFeedbackIndexesSql, "Indices asegurados: sys_mapeo.LocationNormalizationFeedback");
        }

        private static void ExecuteSql(SqlConnection conn, string sql, string logMessage)
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
            Console.WriteLine($"[DbSchema] {logMessage}");
        }
    }
}
