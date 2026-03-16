/* OpsBase - MVP Fase 1 (bootstrap SQL para sys_opsbase) */

IF OBJECT_ID('[sys_opsbase].[ResourceDefinition]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[ResourceDefinition] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Codigo] NVARCHAR(80) NOT NULL,
        [Nombre] NVARCHAR(120) NOT NULL,
        [Descripcion] NVARCHAR(300) NULL,
        [TrackMode] NVARCHAR(30) NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceDefinition_TrackMode] DEFAULT ('none'),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceDefinition_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceDefinition_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_ResourceDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_sys_opsbase_ResourceDefinition_TrackMode]
            CHECK ([TrackMode] IN ('none','serial','lote','serial_lote'))
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[Location]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[Location] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Codigo] NVARCHAR(80) NOT NULL,
        [Nombre] NVARCHAR(160) NOT NULL,
        [Tipo] NVARCHAR(30) NOT NULL,
        [ParentLocationId] INT NULL,
        [Capacidad] DECIMAL(18,3) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_Location_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_Location_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_Location] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_Location_Parent]
            FOREIGN KEY ([ParentLocationId]) REFERENCES [sys_opsbase].[Location]([Id]),
        CONSTRAINT [CK_sys_opsbase_Location_Tipo]
            CHECK ([Tipo] IN ('nodo','deposito','sector','pasillo','estanteria','nivel','posicion'))
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[ResourceInstance]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[ResourceInstance] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ResourceDefinitionId] INT NOT NULL,
        [CodigoInterno] NVARCHAR(120) NOT NULL,
        [Estado] NVARCHAR(30) NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceInstance_Estado] DEFAULT ('activo'),
        [Serie] NVARCHAR(120) NULL,
        [Lote] NVARCHAR(120) NULL,
        [Vencimiento] DATE NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceInstance_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_ResourceInstance_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_ResourceInstance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_ResourceInstance_ResourceDefinition]
            FOREIGN KEY ([ResourceDefinitionId]) REFERENCES [sys_opsbase].[ResourceDefinition]([Id]),
        CONSTRAINT [CK_sys_opsbase_ResourceInstance_Estado]
            CHECK ([Estado] IN ('activo','inactivo','bloqueado','cuarentena','reparacion','baja'))
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[AttributeDefinition]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[AttributeDefinition] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ResourceDefinitionId] INT NOT NULL,
        [Codigo] NVARCHAR(80) NOT NULL,
        [Nombre] NVARCHAR(120) NOT NULL,
        [DataType] NVARCHAR(30) NOT NULL,
        [IsRequired] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_AttributeDefinition_IsRequired] DEFAULT (0),
        [MaxLength] INT NULL,
        [SortOrder] INT NOT NULL CONSTRAINT [DF_sys_opsbase_AttributeDefinition_SortOrder] DEFAULT (1),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_sys_opsbase_AttributeDefinition_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_AttributeDefinition_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_AttributeDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_AttributeDefinition_ResourceDefinition]
            FOREIGN KEY ([ResourceDefinitionId]) REFERENCES [sys_opsbase].[ResourceDefinition]([Id]),
        CONSTRAINT [CK_sys_opsbase_AttributeDefinition_DataType]
            CHECK ([DataType] IN ('string','int','decimal','bool','datetime','json'))
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[AttributeValue]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[AttributeValue] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ResourceInstanceId] INT NOT NULL,
        [AttributeDefinitionId] INT NOT NULL,
        [ValueString] NVARCHAR(500) NULL,
        [ValueNumber] DECIMAL(18,6) NULL,
        [ValueDateTime] DATETIME2 NULL,
        [ValueBool] BIT NULL,
        [ValueJson] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_AttributeValue_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_sys_opsbase_AttributeValue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_AttributeValue_ResourceInstance]
            FOREIGN KEY ([ResourceInstanceId]) REFERENCES [sys_opsbase].[ResourceInstance]([Id]),
        CONSTRAINT [FK_sys_opsbase_AttributeValue_AttributeDefinition]
            FOREIGN KEY ([AttributeDefinitionId]) REFERENCES [sys_opsbase].[AttributeDefinition]([Id])
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[StockBalance]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[StockBalance] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ResourceInstanceId] INT NOT NULL,
        [LocationId] INT NOT NULL,
        [StockReal] DECIMAL(18,3) NOT NULL CONSTRAINT [DF_sys_opsbase_StockBalance_StockReal] DEFAULT (0),
        [StockReservado] DECIMAL(18,3) NOT NULL CONSTRAINT [DF_sys_opsbase_StockBalance_StockReservado] DEFAULT (0),
        [StockDisponible] DECIMAL(18,3) NOT NULL CONSTRAINT [DF_sys_opsbase_StockBalance_StockDisponible] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_StockBalance_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_StockBalance_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_sys_opsbase_StockBalance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_StockBalance_ResourceInstance]
            FOREIGN KEY ([ResourceInstanceId]) REFERENCES [sys_opsbase].[ResourceInstance]([Id]),
        CONSTRAINT [FK_sys_opsbase_StockBalance_Location]
            FOREIGN KEY ([LocationId]) REFERENCES [sys_opsbase].[Location]([Id]),
        CONSTRAINT [CK_sys_opsbase_StockBalance_NoNegativos]
            CHECK ([StockReal] >= 0 AND [StockReservado] >= 0 AND [StockDisponible] >= 0),
        CONSTRAINT [CK_sys_opsbase_StockBalance_ReservadoMenorIgualReal]
            CHECK ([StockReservado] <= [StockReal])
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[Movement]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[Movement] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [MovementType] NVARCHAR(30) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_sys_opsbase_Movement_Status] DEFAULT ('confirmado'),
        [SourceLocationId] INT NULL,
        [TargetLocationId] INT NULL,
        [ReferenceNo] NVARCHAR(80) NULL,
        [Notes] NVARCHAR(500) NULL,
        [OperationAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_Movement_OperationAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_Movement_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_sys_opsbase_Movement] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_Movement_SourceLocation]
            FOREIGN KEY ([SourceLocationId]) REFERENCES [sys_opsbase].[Location]([Id]),
        CONSTRAINT [FK_sys_opsbase_Movement_TargetLocation]
            FOREIGN KEY ([TargetLocationId]) REFERENCES [sys_opsbase].[Location]([Id]),
        CONSTRAINT [CK_sys_opsbase_Movement_MovementType]
            CHECK ([MovementType] IN ('ingreso','egreso','transferencia','ajuste','reserva','liberacion')),
        CONSTRAINT [CK_sys_opsbase_Movement_Status]
            CHECK ([Status] IN ('borrador','confirmado','anulado'))
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[MovementLine]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[MovementLine] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [MovementId] INT NOT NULL,
        [ResourceInstanceId] INT NOT NULL,
        [Quantity] DECIMAL(18,3) NOT NULL,
        [UnitCost] DECIMAL(18,4) NULL,
        [Serie] NVARCHAR(120) NULL,
        [Lote] NVARCHAR(120) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_MovementLine_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_sys_opsbase_MovementLine] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sys_opsbase_MovementLine_Movement]
            FOREIGN KEY ([MovementId]) REFERENCES [sys_opsbase].[Movement]([Id]),
        CONSTRAINT [FK_sys_opsbase_MovementLine_ResourceInstance]
            FOREIGN KEY ([ResourceInstanceId]) REFERENCES [sys_opsbase].[ResourceInstance]([Id]),
        CONSTRAINT [CK_sys_opsbase_MovementLine_QuantityPositiva]
            CHECK ([Quantity] > 0)
    );
END
GO

IF OBJECT_ID('[sys_opsbase].[OperationAudit]', 'U') IS NULL
BEGIN
    CREATE TABLE [sys_opsbase].[OperationAudit] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OperationName] NVARCHAR(80) NOT NULL,
        [EntityName] NVARCHAR(80) NOT NULL,
        [EntityId] INT NULL,
        [Result] NVARCHAR(20) NOT NULL,
        [Message] NVARCHAR(500) NULL,
        [Actor] NVARCHAR(100) NULL,
        [CorrelationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_sys_opsbase_OperationAudit_CorrelationId] DEFAULT (NEWID()),
        [PayloadJson] NVARCHAR(MAX) NULL,
        [ExecutedAt] DATETIME2 NOT NULL CONSTRAINT [DF_sys_opsbase_OperationAudit_ExecutedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_sys_opsbase_OperationAudit] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_sys_opsbase_OperationAudit_Result]
            CHECK ([Result] IN ('ok','error','warning'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_ResourceDefinition_Codigo'
      AND object_id = OBJECT_ID('[sys_opsbase].[ResourceDefinition]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_ResourceDefinition_Codigo]
    ON [sys_opsbase].[ResourceDefinition] ([Codigo]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_Location_Codigo'
      AND object_id = OBJECT_ID('[sys_opsbase].[Location]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_Location_Codigo]
    ON [sys_opsbase].[Location] ([Codigo]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_Location_ParentLocationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[Location]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_Location_ParentLocationId]
    ON [sys_opsbase].[Location] ([ParentLocationId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_ResourceInstance_ResourceDefinitionId_CodigoInterno'
      AND object_id = OBJECT_ID('[sys_opsbase].[ResourceInstance]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_ResourceInstance_ResourceDefinitionId_CodigoInterno]
    ON [sys_opsbase].[ResourceInstance] ([ResourceDefinitionId], [CodigoInterno]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_AttributeDefinition_ResourceDefinitionId_Codigo'
      AND object_id = OBJECT_ID('[sys_opsbase].[AttributeDefinition]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_AttributeDefinition_ResourceDefinitionId_Codigo]
    ON [sys_opsbase].[AttributeDefinition] ([ResourceDefinitionId], [Codigo]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_AttributeDefinition_ResourceDefinitionId'
      AND object_id = OBJECT_ID('[sys_opsbase].[AttributeDefinition]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_AttributeDefinition_ResourceDefinitionId]
    ON [sys_opsbase].[AttributeDefinition] ([ResourceDefinitionId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_AttributeValue_ResourceInstanceId_AttributeDefinitionId'
      AND object_id = OBJECT_ID('[sys_opsbase].[AttributeValue]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_AttributeValue_ResourceInstanceId_AttributeDefinitionId]
    ON [sys_opsbase].[AttributeValue] ([ResourceInstanceId], [AttributeDefinitionId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_StockBalance_LocationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[StockBalance]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_StockBalance_LocationId]
    ON [sys_opsbase].[StockBalance] ([LocationId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_sys_opsbase_StockBalance_ResourceInstanceId_LocationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[StockBalance]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_sys_opsbase_StockBalance_ResourceInstanceId_LocationId]
    ON [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_Movement_SourceLocationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[Movement]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_Movement_SourceLocationId]
    ON [sys_opsbase].[Movement] ([SourceLocationId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_Movement_TargetLocationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[Movement]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_Movement_TargetLocationId]
    ON [sys_opsbase].[Movement] ([TargetLocationId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_Movement_MovementType_Status'
      AND object_id = OBJECT_ID('[sys_opsbase].[Movement]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_Movement_MovementType_Status]
    ON [sys_opsbase].[Movement] ([MovementType], [Status]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_MovementLine_MovementId'
      AND object_id = OBJECT_ID('[sys_opsbase].[MovementLine]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_MovementLine_MovementId]
    ON [sys_opsbase].[MovementLine] ([MovementId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_MovementLine_ResourceInstanceId'
      AND object_id = OBJECT_ID('[sys_opsbase].[MovementLine]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_MovementLine_ResourceInstanceId]
    ON [sys_opsbase].[MovementLine] ([ResourceInstanceId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_OperationAudit_ExecutedAt'
      AND object_id = OBJECT_ID('[sys_opsbase].[OperationAudit]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_OperationAudit_ExecutedAt]
    ON [sys_opsbase].[OperationAudit] ([ExecutedAt]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_sys_opsbase_OperationAudit_CorrelationId'
      AND object_id = OBJECT_ID('[sys_opsbase].[OperationAudit]')
)
BEGIN
    CREATE INDEX [IX_sys_opsbase_OperationAudit_CorrelationId]
    ON [sys_opsbase].[OperationAudit] ([CorrelationId]);
END
GO
