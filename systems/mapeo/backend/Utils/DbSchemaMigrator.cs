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
    }
}
