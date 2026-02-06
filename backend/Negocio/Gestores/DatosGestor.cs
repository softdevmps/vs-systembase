using System.Data;
using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class DatosGestor
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "string",
            "int",
            "decimal",
            "bool",
            "datetime",
            "guid"
        };

        public static (bool Ok, string? Error, List<Dictionary<string, object?>> Data) Listar(int systemId, int entityId, int? take = null)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error, new List<Dictionary<string, object?>>());

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;

            var columns = fields.Select(f => $"[{f.ColumnName}]");
            var sql = new StringBuilder();
            sql.Append($"SELECT {string.Join(", ", columns)} FROM [{schemaName}].[{entity.TableName}]");
            if (take.HasValue && take.Value > 0)
            {
                var pkColumn = pk?.ColumnName ?? fields.First().ColumnName;
                sql.Append($" ORDER BY [{pkColumn}] OFFSET 0 ROWS FETCH NEXT {take.Value} ROWS ONLY");
            }

            var result = new List<Dictionary<string, object?>>();

            using var conn = context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in fields)
                {
                    var value = reader[field.ColumnName];
                    row[field.ColumnName] = value == DBNull.Value ? null : value;
                }

                result.Add(row);
            }

            return (true, null, result);
        }

        public static (bool Ok, string? Error) Crear(int systemId, int entityId, Dictionary<string, JsonElement> data)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;

            var columns = new List<string>();
            var parameters = new List<IDbDataParameter>();
            var values = new List<string>();

            foreach (var field in fields)
            {
                if (field.IsIdentity)
                    continue;

                var key = field.ColumnName;
                if (!data.TryGetValue(key, out var element))
                {
                    if (field.Required || field.IsPrimaryKey)
                        return (false, $"Campo requerido: {field.ColumnName}");
                    continue;
                }

                var value = ConvertValue(element, field);
                if (value == InvalidValue)
                    return (false, $"Valor invalido para {field.ColumnName}");

                var paramName = $"@p{parameters.Count}";
                columns.Add($"[{field.ColumnName}]");
                values.Add(paramName);
                parameters.Add(CreateParameter(paramName, value));
            }

            if (columns.Count == 0)
                return (false, "No hay columnas para insertar.");

            var sql = $"INSERT INTO [{schemaName}].[{entity.TableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";

            ExecuteNonQuery(context, sql, parameters);
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int systemId, int entityId, string id, Dictionary<string, JsonElement> data)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;
            if (pk == null)
                return (false, "Entidad sin PK.");

            var setParts = new List<string>();
            var parameters = new List<IDbDataParameter>();

            foreach (var field in fields)
            {
                if (field.IsPrimaryKey || field.IsIdentity)
                    continue;

                var key = field.ColumnName;
                if (!data.TryGetValue(key, out var element))
                    continue;

                var value = ConvertValue(element, field);
                if (value == InvalidValue)
                    return (false, $"Valor invalido para {field.ColumnName}");

                var paramName = $"@p{parameters.Count}";
                setParts.Add($"[{field.ColumnName}] = {paramName}");
                parameters.Add(CreateParameter(paramName, value));
            }

            if (setParts.Count == 0)
                return (false, "No hay columnas para actualizar.");

            var pkValue = ConvertPrimaryKey(id, pk);
            if (pkValue == InvalidValue)
                return (false, "Id invalido.");

            var pkParam = $"@p{parameters.Count}";
            parameters.Add(CreateParameter(pkParam, pkValue));

            var sql = $"UPDATE [{schemaName}].[{entity.TableName}] SET {string.Join(", ", setParts)} WHERE [{pk.ColumnName}] = {pkParam};";
            ExecuteNonQuery(context, sql, parameters);
            return (true, null);
        }

        public static (bool Ok, string? Error) Eliminar(int systemId, int entityId, string id)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;
            if (pk == null)
                return (false, "Entidad sin PK.");

            var pkValue = ConvertPrimaryKey(id, pk);
            if (pkValue == InvalidValue)
                return (false, "Id invalido.");

            var pkParam = "@p0";
            var sql = $"DELETE FROM [{schemaName}].[{entity.TableName}] WHERE [{pk.ColumnName}] = {pkParam};";
            ExecuteNonQuery(context, sql, new[] { CreateParameter(pkParam, pkValue) });
            return (true, null);
        }

        private static void ExecuteNonQuery(SystemBaseContext context, string sql, IEnumerable<IDbDataParameter> parameters)
        {
            using var conn = context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            cmd.ExecuteNonQuery();
        }

        private static (bool Ok, string? Error, string SchemaName, Entities Entity, List<Fields> Fields, Fields? Pk) LoadMetadata(SystemBaseContext context, int systemId, int entityId)
        {
            var system = context.Systems.FirstOrDefault(s => s.Id == systemId);
            if (system == null)
                return (false, "Sistema no encontrado.", "", null!, new List<Fields>(), null);

            var schemaName = ToSafeSchemaName(system.Slug);
            if (schemaName == null)
                return (false, "Slug invalido.", "", null!, new List<Fields>(), null);

            var entity = context.Entities
                .Include(e => e.Fields)
                .FirstOrDefault(e => e.Id == entityId && e.SystemId == systemId);

            if (entity == null)
                return (false, "Entidad no encontrada.", "", null!, new List<Fields>(), null);

            if (ToSafeSqlName(entity.TableName) == null)
                return (false, "TableName invalido.", "", null!, new List<Fields>(), null);

            var fields = entity.Fields
                .Where(f => AllowedTypes.Contains(f.DataType))
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .ToList();

            if (fields.Count == 0)
                return (false, "Entidad sin campos.", "", null!, new List<Fields>(), null);

            foreach (var field in fields)
            {
                if (ToSafeSqlName(field.ColumnName) == null)
                    return (false, $"ColumnName invalido: {field.ColumnName}", "", null!, new List<Fields>(), null);
            }

            var pk = fields.FirstOrDefault(f => f.IsPrimaryKey);
            return (true, null, schemaName, entity, fields, pk);
        }

        private static readonly object InvalidValue = new();

        private static object ConvertPrimaryKey(string id, Fields pk)
        {
            if (pk.DataType.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(id, out var v) ? v : InvalidValue;
            }

            if (pk.DataType.Equals("guid", StringComparison.OrdinalIgnoreCase))
            {
                return Guid.TryParse(id, out var v) ? v : InvalidValue;
            }

            return id;
        }

        private static object ConvertValue(JsonElement element, Fields field)
        {
            var type = field.DataType.ToLowerInvariant();

            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return DBNull.Value;

            try
            {
                return type switch
                {
                    "string" => element.GetString() ?? string.Empty,
                    "int" => element.ValueKind == JsonValueKind.Number ? element.GetInt32() : int.Parse(element.GetString() ?? "0"),
                    "decimal" => element.ValueKind == JsonValueKind.Number ? element.GetDecimal() : decimal.Parse(element.GetString() ?? "0"),
                    "bool" => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
                        ? element.GetBoolean()
                        : bool.Parse(element.GetString() ?? "false"),
                    "datetime" => element.ValueKind == JsonValueKind.String ? DateTime.Parse(element.GetString() ?? string.Empty) : element.GetDateTime(),
                    "guid" => Guid.Parse(element.GetString() ?? string.Empty),
                    _ => InvalidValue
                };
            }
            catch
            {
                return InvalidValue;
            }
        }

        private static IDbDataParameter CreateParameter(string name, object value)
        {
            var param = new Microsoft.Data.SqlClient.SqlParameter
            {
                ParameterName = name,
                Value = value ?? DBNull.Value
            };

            return param;
        }

        private static string? ToSafeSchemaName(string slug)
        {
            return ToSafeSqlName($"sys_{slug}");
        }

        private static string? ToSafeSqlName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();
            var sb = new StringBuilder();

            foreach (var ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    sb.Append(ch);
                }
                else
                {
                    return null;
                }
            }

            if (sb.Length == 0)
                return null;

            if (char.IsDigit(sb[0]))
                return null;

            return sb.ToString();
        }
    }
}
