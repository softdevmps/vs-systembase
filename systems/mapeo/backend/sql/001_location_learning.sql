-- Mapeo - Normalizacion/feedback de ubicaciones (idempotente)
-- Ejecutar en base systemBase.

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
END;

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
END;

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
END;

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
END;

-- Semillas opcionales de reglas frecuentes:
IF NOT EXISTS (
    SELECT 1 FROM [sys_mapeo].[LocationNormalizationRules]
    WHERE [FindText] = N'alverdi' AND [Scope] = N'location' AND [IsActive] = 1
)
BEGIN
    INSERT INTO [sys_mapeo].[LocationNormalizationRules]
        ([FindText], [ReplaceText], [Scope], [Priority], [IsRegex], [IsActive], [Source])
    VALUES
        (N'alverdi', N'alberdi', N'location', 10, 0, 1, N'seed');
END;

IF NOT EXISTS (
    SELECT 1 FROM [sys_mapeo].[LocationNormalizationRules]
    WHERE [FindText] = N'rondeo' AND [Scope] = N'location' AND [IsActive] = 1
)
BEGIN
    INSERT INTO [sys_mapeo].[LocationNormalizationRules]
        ([FindText], [ReplaceText], [Scope], [Priority], [IsRegex], [IsActive], [Source])
    VALUES
        (N'rondeo', N'rondeau', N'location', 10, 0, 1, N'seed');
END;

IF NOT EXISTS (
    SELECT 1 FROM [sys_mapeo].[LocationNormalizationRules]
    WHERE [FindText] = N'fuerza area' AND [Scope] = N'location' AND [IsActive] = 1
)
BEGIN
    INSERT INTO [sys_mapeo].[LocationNormalizationRules]
        ([FindText], [ReplaceText], [Scope], [Priority], [IsRegex], [IsActive], [Source])
    VALUES
        (N'fuerza area', N'fuerza aerea', N'location', 10, 0, 1, N'seed');
END;
