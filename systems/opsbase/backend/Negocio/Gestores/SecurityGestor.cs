using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public class SecurityUserPermissions
    {
        public bool Enabled { get; set; }
        public bool IsAdmin { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public static class SecurityGestor
    {
        public static bool HasPermission(string? usuario, string? permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                return true;

            var context = GetUserPermissions(usuario);
            if (!context.Enabled)
                return true;
            if (context.IsAdmin)
                return true;

            var normalized = permissionCode.Trim().ToLowerInvariant();
            return context.Permissions.Contains(normalized);
        }

        public static SecurityUserPermissions GetUserPermissions(string? usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                return new SecurityUserPermissions { Enabled = false, IsAdmin = false };

            using var conn = Db.Open();
            if (!IsPermissionModelReady(conn))
                return new SecurityUserPermissions { Enabled = false, IsAdmin = false };

            return LoadUserPermissions(conn, usuario);
        }

        private static SecurityUserPermissions LoadUserPermissions(SqlConnection conn, string usuario)
        {
            const string sql = @"
SELECT DISTINCT
       LOWER(r.[Codigo]) AS RoleCode,
       LOWER(p.[Codigo]) AS PermissionCode
FROM [sys_opsbase].[UserRole] ur
INNER JOIN [sys_opsbase].[Role] r
        ON r.[Id] = ur.[RoleId]
       AND r.[IsActive] = 1
LEFT JOIN [sys_opsbase].[RolePermission] rp
       ON rp.[RoleId] = r.[Id]
      AND rp.[IsActive] = 1
LEFT JOIN [sys_opsbase].[Permission] p
       ON p.[Id] = rp.[PermissionId]
      AND p.[IsActive] = 1
WHERE ur.[IsActive] = 1
  AND LOWER(ur.[Usuario]) = LOWER(@usuario);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@usuario", usuario);
            using var reader = cmd.ExecuteReader();

            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var roleCode = reader["RoleCode"] == DBNull.Value ? null : reader["RoleCode"]?.ToString();
                var permissionCode = reader["PermissionCode"] == DBNull.Value ? null : reader["PermissionCode"]?.ToString();

                if (!string.IsNullOrWhiteSpace(roleCode))
                    roles.Add(roleCode.Trim().ToLowerInvariant());
                if (!string.IsNullOrWhiteSpace(permissionCode))
                    permissions.Add(permissionCode.Trim().ToLowerInvariant());
            }

            var isAdminByUser = string.Equals(usuario, "admin", StringComparison.OrdinalIgnoreCase);
            var isAdminByRole = roles.Contains("ops.admin") || roles.Contains("admin");
            var isAdmin = isAdminByUser || isAdminByRole;

            if (isAdmin)
                permissions.Add("*");

            return new SecurityUserPermissions
            {
                Enabled = true,
                IsAdmin = isAdmin,
                Permissions = permissions.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        private static bool IsPermissionModelReady(SqlConnection conn)
        {
            const string sql = @"
SELECT CASE WHEN
    OBJECT_ID('[sys_opsbase].[Role]', 'U') IS NOT NULL
    AND OBJECT_ID('[sys_opsbase].[Permission]', 'U') IS NOT NULL
    AND OBJECT_ID('[sys_opsbase].[RolePermission]', 'U') IS NOT NULL
    AND OBJECT_ID('[sys_opsbase].[UserRole]', 'U') IS NOT NULL
THEN 1 ELSE 0 END;";

            using var cmd = new SqlCommand(sql, conn);
            var result = Convert.ToInt32(cmd.ExecuteScalar());
            return result == 1;
        }
    }
}
