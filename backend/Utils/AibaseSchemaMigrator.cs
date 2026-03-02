using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Utils
{
    public static class AibaseSchemaMigrator
    {
        public static void EnsureSchema(IServiceProvider services, ILogger logger)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SystemBaseContext>();

                if (!context.Database.CanConnect())
                {
                    logger.LogWarning("AibaseSchemaMigrator: no se pudo conectar a la base de datos.");
                    return;
                }

                context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sb_ai')
    EXEC('CREATE SCHEMA sb_ai');

IF OBJECT_ID('sb_ai.Templates', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Templates
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(80) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        PipelineJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_sb_ai_Templates_PipelineJson DEFAULT('{}'),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_ai_Templates_IsActive DEFAULT(1),
        [Version] NVARCHAR(20) NOT NULL CONSTRAINT DF_sb_ai_Templates_Version DEFAULT('1.0'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Templates_CreatedAt DEFAULT(SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UX_sb_ai_Templates_Key UNIQUE ([Key])
    );
END;

IF OBJECT_ID('sb_ai.Projects', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Projects
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Slug NVARCHAR(80) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Language] NVARCHAR(10) NOT NULL CONSTRAINT DF_sb_ai_Projects_Language DEFAULT('es'),
        Tone NVARCHAR(100) NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Projects_Status DEFAULT('draft'),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_ai_Projects_IsActive DEFAULT(1),
        TemplateId INT NOT NULL,
        TenantId INT NULL,
        CreatedByUserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Projects_CreatedAt DEFAULT(SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UX_sb_ai_Projects_Slug UNIQUE (Slug),
        CONSTRAINT FK_sb_ai_Projects_Template FOREIGN KEY (TemplateId) REFERENCES sb_ai.Templates(Id)
    );
END;

IF OBJECT_ID('sb_ai.Runs', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Runs
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectId INT NOT NULL,
        RunType NVARCHAR(50) NOT NULL CONSTRAINT DF_sb_ai_Runs_RunType DEFAULT('dataset_build'),
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Runs_Status DEFAULT('queued'),
        EngineRunId NVARCHAR(120) NULL,
        ProgressPct INT NOT NULL CONSTRAINT DF_sb_ai_Runs_ProgressPct DEFAULT(0),
        RequestedByUserId INT NOT NULL,
        TriggerSource NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Runs_TriggerSource DEFAULT('manual'),
        InputJson NVARCHAR(MAX) NULL,
        OutputJson NVARCHAR(MAX) NULL,
        LastError NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Runs_CreatedAt DEFAULT(SYSDATETIME()),
        StartedAt DATETIME2 NULL,
        FinishedAt DATETIME2 NULL,
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_sb_ai_Runs_Project FOREIGN KEY (ProjectId) REFERENCES sb_ai.Projects(Id)
    );
END;
");

                context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM sb_ai.Templates WHERE [Key] = 'extractor-json')
BEGIN
    INSERT INTO sb_ai.Templates ([Key], [Name], [Description], PipelineJson, IsActive, [Version])
    VALUES (
        'extractor-json',
        'Extractor JSON',
        'Extrae estructura JSON validada por schema.',
        '{""key"":""extractor-json"",""steps"": [{""name"":""dataset_build"",""engine"":""/dataset/build""},{""name"":""train_lora"",""engine"":""/train/lora""},{""name"":""eval"",""engine"":""/eval/run""}],""supports"": [""versioning"",""rollback""]}',
        1,
        '1.0'
    );
END;

IF NOT EXISTS (SELECT 1 FROM sb_ai.Templates WHERE [Key] = 'chat-rag')
BEGIN
    INSERT INTO sb_ai.Templates ([Key], [Name], [Description], PipelineJson, IsActive, [Version])
    VALUES (
        'chat-rag',
        'Chat RAG',
        'Chat con contexto documental e indice vectorial.',
        '{""key"":""chat-rag"",""steps"": [{""name"":""dataset_build"",""engine"":""/dataset/build""},{""name"":""rag_index"",""engine"":""/rag/index""},{""name"":""eval"",""engine"":""/eval/run""}],""supports"": [""versioning"",""rollback""]}',
        1,
        '1.0'
    );
END;
");

                logger.LogInformation("AibaseSchemaMigrator: schema sb_ai validado y templates base disponibles.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AibaseSchemaMigrator: error al crear esquema sb_ai.");
            }
        }
    }
}
