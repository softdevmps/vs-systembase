using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class RubroSchemaHelper
    {
        public static void EnsureSchema(SqlConnection conn, SqlTransaction? tx = null)
        {
            const string sql = @"
IF OBJECT_ID('[sys_opsbase].[Rubro]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[Rubro] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Codigo] NVARCHAR(60) NOT NULL,
        [Nombre] NVARCHAR(120) NOT NULL,
        [Descripcion] NVARCHAR(300) NULL,
        [ColorHex] NVARCHAR(20) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_Rubro_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_Rubro_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_Rubro] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'UX_sys_opsbase_Rubro_Codigo'
      AND [object_id] = OBJECT_ID('[sys_opsbase].[Rubro]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_Rubro_Codigo]
    ON [sys_opsbase].[Rubro] ([Codigo]);
END;

IF COL_LENGTH('[sys_opsbase].[ResourceDefinition]', 'RubroId') IS NULL
BEGIN
    ALTER TABLE [sys_opsbase].[ResourceDefinition]
    ADD [RubroId] INT NULL;
END;

IF COL_LENGTH('[sys_opsbase].[Location]', 'RubroId') IS NULL
BEGIN
    ALTER TABLE [sys_opsbase].[Location]
    ADD [RubroId] INT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = 'FK_sys_opsbase_ResourceDefinition_Rubro'
      AND [parent_object_id] = OBJECT_ID('[sys_opsbase].[ResourceDefinition]')
)
BEGIN
    ALTER TABLE [sys_opsbase].[ResourceDefinition]
    ADD CONSTRAINT [FK_sys_opsbase_ResourceDefinition_Rubro]
    FOREIGN KEY ([RubroId]) REFERENCES [sys_opsbase].[Rubro]([Id]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = 'FK_sys_opsbase_Location_Rubro'
      AND [parent_object_id] = OBJECT_ID('[sys_opsbase].[Location]')
)
BEGIN
    ALTER TABLE [sys_opsbase].[Location]
    ADD CONSTRAINT [FK_sys_opsbase_Location_Rubro]
    FOREIGN KEY ([RubroId]) REFERENCES [sys_opsbase].[Rubro]([Id]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'IX_sys_opsbase_ResourceDefinition_RubroId'
      AND [object_id] = OBJECT_ID('[sys_opsbase].[ResourceDefinition]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_ResourceDefinition_RubroId]
    ON [sys_opsbase].[ResourceDefinition] ([RubroId]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'IX_sys_opsbase_Location_RubroId'
      AND [object_id] = OBJECT_ID('[sys_opsbase].[Location]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_Location_RubroId]
    ON [sys_opsbase].[Location] ([RubroId]);
END;

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Rubro] WHERE [Codigo] = 'GENERAL')
BEGIN
    INSERT INTO [sys_opsbase].[Rubro] ([Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive])
    VALUES ('GENERAL', 'General', 'Rubro genérico para operación inicial.', '#64748b', 1);
END;

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Rubro] WHERE [Codigo] = 'FARMACIA')
BEGIN
    INSERT INTO [sys_opsbase].[Rubro] ([Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive])
    VALUES ('FARMACIA', 'Farmacia', 'Medicamentos, insumos y equipamiento médico.', '#16a34a', 1);
END;

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Rubro] WHERE [Codigo] = 'ALIMENTOS')
BEGIN
    INSERT INTO [sys_opsbase].[Rubro] ([Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive])
    VALUES ('ALIMENTOS', 'Alimentos', 'Productos alimenticios y perecederos.', '#f59e0b', 1);
END;

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Rubro] WHERE [Codigo] = 'TECNOLOGIA')
BEGIN
    INSERT INTO [sys_opsbase].[Rubro] ([Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive])
    VALUES ('TECNOLOGIA', 'Tecnología', 'Equipos electrónicos y activos tecnológicos.', '#2563eb', 1);
END;

DECLARE @GeneralRubroId INT = (
    SELECT TOP 1 [Id] FROM [sys_opsbase].[Rubro] WHERE [Codigo] = 'GENERAL'
);

IF @GeneralRubroId IS NOT NULL
BEGIN
    UPDATE [sys_opsbase].[ResourceDefinition]
    SET [RubroId] = @GeneralRubroId
    WHERE [RubroId] IS NULL;

    UPDATE [sys_opsbase].[Location]
    SET [RubroId] = @GeneralRubroId
    WHERE [RubroId] IS NULL;
END;";

            using var cmd = tx == null
                ? new SqlCommand(sql, conn)
                : new SqlCommand(sql, conn, tx);
            cmd.ExecuteNonQuery();
        }

        public static bool ExistsActiveRubro(SqlConnection conn, int rubroId, SqlTransaction? tx = null)
        {
            const string sql = @"SELECT TOP 1 1
FROM [sys_opsbase].[Rubro]
WHERE [Id] = @id
  AND [IsActive] = 1;";

            using var cmd = tx == null
                ? new SqlCommand(sql, conn)
                : new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", rubroId);
            return cmd.ExecuteScalar() != null;
        }
    }
}
