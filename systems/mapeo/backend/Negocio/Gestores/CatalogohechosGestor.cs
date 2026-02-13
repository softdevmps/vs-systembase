using Backend.Data;
using Backend.Models.Catalogohechos;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class CatalogohechosGestor
    {
        public static List<CatalogohechosResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [Codigo], [Nombre], [Categoria], [Subcategoria], [PalabrasClave], [Activo], [CreatedAt] FROM [sys_mapeo].[CatalogoHechos]");
            sql.Append(" WHERE [Activo] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<CatalogohechosResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static CatalogohechosResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [Codigo], [Nombre], [Categoria], [Subcategoria], [PalabrasClave], [Activo], [CreatedAt] FROM [sys_mapeo].[CatalogoHechos] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(CatalogohechosCreateRequest request)
        {
            using var conn = Db.Open();
            if (request.Codigo != null && request.Codigo.Length > 50) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_mapeo", "CatalogoHechos", "Codigo", request.Codigo!, null, null)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 200) return (false, "MaxLength excedido: Nombre");
            if (request.Categoria != null && request.Categoria.Length > 100) return (false, "MaxLength excedido: Categoria");
            if (request.Subcategoria != null && request.Subcategoria.Length > 100) return (false, "MaxLength excedido: Subcategoria");
            if (request.Palabrasclave != null && request.Palabrasclave.Length > 4000) return (false, "MaxLength excedido: PalabrasClave");

            var sql = "INSERT INTO [sys_mapeo].[CatalogoHechos] ([Codigo], [Nombre], [Categoria], [Subcategoria], [PalabrasClave], [Activo], [CreatedAt]) VALUES (@Codigo, @Nombre, @Categoria, @Subcategoria, @PalabrasClave, @Activo, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Categoria", request.Categoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Subcategoria", request.Subcategoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PalabrasClave", request.Palabrasclave ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Activo", request.Activo);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, CatalogohechosUpdateRequest request)
        {
            using var conn = Db.Open();
            if (request.Codigo != null && request.Codigo.Length > 50) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_mapeo", "CatalogoHechos", "Codigo", request.Codigo!, "Id", id)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 200) return (false, "MaxLength excedido: Nombre");
            if (request.Categoria != null && request.Categoria.Length > 100) return (false, "MaxLength excedido: Categoria");
            if (request.Subcategoria != null && request.Subcategoria.Length > 100) return (false, "MaxLength excedido: Subcategoria");
            if (request.Palabrasclave != null && request.Palabrasclave.Length > 4000) return (false, "MaxLength excedido: PalabrasClave");
            var sql = "UPDATE [sys_mapeo].[CatalogoHechos] SET [Codigo] = @Codigo, [Nombre] = @Nombre, [Categoria] = @Categoria, [Subcategoria] = @Subcategoria, [PalabrasClave] = @PalabrasClave, [Activo] = @Activo, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Categoria", request.Categoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Subcategoria", request.Subcategoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PalabrasClave", request.Palabrasclave ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Activo", request.Activo);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "UPDATE [sys_mapeo].[CatalogoHechos] SET [Activo] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static CatalogohechosResponse MapToResponse(SqlDataReader reader)
        {
            return new CatalogohechosResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Codigo = reader["Codigo"] == DBNull.Value ? null : reader["Codigo"].ToString(),
                Nombre = reader["Nombre"] == DBNull.Value ? null : reader["Nombre"].ToString(),
                Categoria = reader["Categoria"] == DBNull.Value ? null : reader["Categoria"].ToString(),
                Subcategoria = reader["Subcategoria"] == DBNull.Value ? null : reader["Subcategoria"].ToString(),
                Palabrasclave = reader["PalabrasClave"] == DBNull.Value ? null : reader["PalabrasClave"].ToString(),
                Activo = reader["Activo"] == DBNull.Value ? default(bool) : (bool)Convert.ChangeType(reader["Activo"], typeof(bool)),
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
