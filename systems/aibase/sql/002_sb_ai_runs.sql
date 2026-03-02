/* AIBase V1 - tabla de runs/orquestacion */

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
GO
