-- ReconcDB Table Creation Script
-- Database: ReconcDB on SGPROD-SQL02\SQLEXPRESS
-- Size: ~15 GB
-- Service Account: svc_recon@voyager.local

USE ReconcDB;
GO

-- BatchRuns: Tracks each nightly reconciliation run
CREATE TABLE [dbo].[BatchRuns]
(
    [BatchRunId]        UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY,
    [StartTime]         DATETIME2(7)        NOT NULL,
    [EndTime]           DATETIME2(7)        NULL,
    [Status]            VARCHAR(50)         NOT NULL DEFAULT 'Running',
    [CorrelationId]     VARCHAR(50)         NOT NULL,
    [MatchedCount]      INT                 NOT NULL DEFAULT 0,
    [DiscrepancyCount]  INT                 NOT NULL DEFAULT 0,
    [FailedCount]       INT                 NOT NULL DEFAULT 0,
    [ErrorMessage]      NVARCHAR(2000)      NULL,
    [CreatedAt]         DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_BatchRuns_StartTime ON [dbo].[BatchRuns] ([StartTime] DESC);
CREATE INDEX IX_BatchRuns_Status ON [dbo].[BatchRuns] ([Status]);
GO

-- ReconcResults: Individual match results for each settlement record
CREATE TABLE [dbo].[ReconcResults]
(
    [MatchResultId]         UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY,
    [BatchRunId]            UNIQUEIDENTIFIER    NULL,
    [SettlementRecordId]    UNIQUEIDENTIFIER    NOT NULL,
    [TransactionId]         UNIQUEIDENTIFIER    NOT NULL,
    [Status]                VARCHAR(50)         NOT NULL,
    [SettlementAmount]      DECIMAL(18, 4)      NOT NULL,
    [TransactionAmount]     DECIMAL(18, 4)      NOT NULL,
    [DiscrepancyAmount]     DECIMAL(18, 4)      NOT NULL DEFAULT 0,
    [MatchedOn]             DATETIME2(7)        NOT NULL,
    [MatchCriteria]         VARCHAR(200)        NULL,
    [RetryCount]            INT                 NOT NULL DEFAULT 0,
    [LastRetryTime]         DATETIME2(7)        NULL,
    [CreatedAt]             DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_ReconcResults_BatchRunId ON [dbo].[ReconcResults] ([BatchRunId]);
CREATE INDEX IX_ReconcResults_TransactionId ON [dbo].[ReconcResults] ([TransactionId]);
CREATE INDEX IX_ReconcResults_Status ON [dbo].[ReconcResults] ([Status]);
CREATE INDEX IX_ReconcResults_MatchedOn ON [dbo].[ReconcResults] ([MatchedOn] DESC);
GO

-- Discrepancies: Detected issues requiring attention
CREATE TABLE [dbo].[Discrepancies]
(
    [DiscrepancyId]         UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY,
    [BatchRunId]            UNIQUEIDENTIFIER    NOT NULL,
    [Type]                  VARCHAR(50)         NOT NULL,
    [SettlementRecordId]    UNIQUEIDENTIFIER    NULL,
    [TransactionId]         UNIQUEIDENTIFIER    NULL,
    [SettlementAmount]      DECIMAL(18, 4)      NOT NULL DEFAULT 0,
    [TransactionAmount]     DECIMAL(18, 4)      NOT NULL DEFAULT 0,
    [DiscrepancyAmount]     DECIMAL(18, 4)      NOT NULL,
    [DetectedAt]            DATETIME2(7)        NOT NULL,
    [Description]           NVARCHAR(1000)      NULL,
    [IsResolved]            BIT                 NOT NULL DEFAULT 0,
    [ResolvedAt]            DATETIME2(7)        NULL,
    [ResolvedBy]            NVARCHAR(100)       NULL,
    [CreatedAt]             DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_Discrepancies_BatchRunId ON [dbo].[Discrepancies] ([BatchRunId]);
CREATE INDEX IX_Discrepancies_Type ON [dbo].[Discrepancies] ([Type]);
CREATE INDEX IX_Discrepancies_IsResolved ON [dbo].[Discrepancies] ([IsResolved]);
GO

-- MatchHistory: Audit trail of all match attempts (for retry tracking)
CREATE TABLE [dbo].[MatchHistory]
(
    [HistoryId]             UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [MatchResultId]         UNIQUEIDENTIFIER    NOT NULL,
    [AttemptNumber]         INT                 NOT NULL,
    [Status]                VARCHAR(50)         NOT NULL,
    [AttemptTime]           DATETIME2(7)        NOT NULL,
    [ErrorMessage]          NVARCHAR(1000)      NULL,
    [DurationMs]            INT                 NULL
);
GO

CREATE INDEX IX_MatchHistory_MatchResultId ON [dbo].[MatchHistory] ([MatchResultId]);
GO

-- RetryQueue: Items pending retry by the RetryHandler service
CREATE TABLE [dbo].[RetryQueue]
(
    [RetryQueueId]          UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ReconcResultId]        UNIQUEIDENTIFIER    NOT NULL,
    [TransactionId]         UNIQUEIDENTIFIER    NOT NULL,
    [SettlementRecordId]    UNIQUEIDENTIFIER    NOT NULL,
    [RetryCount]            INT                 NOT NULL DEFAULT 0,
    [MaxRetries]            INT                 NOT NULL DEFAULT 3,
    [LastAttemptTime]       DATETIME2(7)        NULL,
    [NextRetryTime]         DATETIME2(7)        NOT NULL,
    [FailureReason]         NVARCHAR(500)       NULL,
    [Status]                VARCHAR(50)         NOT NULL DEFAULT 'Pending',
    [CreatedAt]             DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_RetryQueue_Status ON [dbo].[RetryQueue] ([Status]);
CREATE INDEX IX_RetryQueue_NextRetryTime ON [dbo].[RetryQueue] ([NextRetryTime]);
GO
