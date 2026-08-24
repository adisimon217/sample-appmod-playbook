-- =============================================
-- MerchantHub Database Schema
-- Database: MerchantHubDB
-- Created: 2017-03-15 by S. Patel
-- Last Modified: 2023-06-22 by J. Rodriguez
-- NOTE: 42 tables total, key tables shown below
-- =============================================

USE [MerchantHubDB]
GO

-- =============================================
-- Core Merchant Tables
-- =============================================

CREATE TABLE [dbo].[MH_Merchants] (
    [MerchantId] INT IDENTITY(1,1) NOT NULL,
    [BusinessName] NVARCHAR(200) NOT NULL,
    [DBA] NVARCHAR(200) NULL,
    [EIN] VARCHAR(11) NULL,
    [MerchantNumber] VARCHAR(20) NULL,
    [Address1] NVARCHAR(200) NOT NULL,
    [Address2] NVARCHAR(200) NULL,
    [City] NVARCHAR(100) NOT NULL,
    [State] CHAR(2) NOT NULL,
    [ZipCode] VARCHAR(10) NOT NULL,
    [Phone] VARCHAR(20) NULL,
    [Email] NVARCHAR(200) NOT NULL,
    [Website] NVARCHAR(500) NULL,
    [Status] VARCHAR(50) NOT NULL DEFAULT 'PendingReview',
    [Tier] VARCHAR(50) NULL DEFAULT 'Standard',
    [MCC] CHAR(4) NULL,
    [MonthlyVolumeLimit] DECIMAL(18,2) NULL,
    [SingleTransactionLimit] DECIMAL(18,2) NULL,
    [ProcessingFeeRate] DECIMAL(5,4) NULL,
    [ContractStartDate] DATETIME2 NOT NULL,
    [ContractEndDate] DATETIME2 NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [LastProfileUpdate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [SuspendedDate] DATETIME2 NULL,
    [SuspensionReason] NVARCHAR(500) NULL,
    [SuspendedBy] NVARCHAR(100) NULL,
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [ModifiedDate] DATETIME2 NULL,
    CONSTRAINT [PK_MH_Merchants] PRIMARY KEY CLUSTERED ([MerchantId] ASC)
)
GO

CREATE NONCLUSTERED INDEX [IX_MH_Merchants_Status] ON [dbo].[MH_Merchants] ([Status]) INCLUDE ([BusinessName], [MerchantNumber])
GO
CREATE NONCLUSTERED INDEX [IX_MH_Merchants_MerchantNumber] ON [dbo].[MH_Merchants] ([MerchantNumber])
GO

-- =============================================
-- User Authentication Tables
-- =============================================

CREATE TABLE [dbo].[MH_Users] (
    [UserId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(256) NOT NULL,
    [PasswordSalt] NVARCHAR(128) NOT NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [Email] NVARCHAR(200) NOT NULL,
    [Role] VARCHAR(50) NOT NULL DEFAULT 'MerchantUser',
    [IsActive] BIT NOT NULL DEFAULT 1,
    [LastLoginDate] DATETIME2 NULL,
    [LastLoginIP] VARCHAR(45) NULL,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIME2 NULL,
    [PasswordResetToken] NVARCHAR(100) NULL,
    [PasswordResetExpiry] DATETIME2 NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_MH_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_MH_Users_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId]),
    CONSTRAINT [UQ_MH_Users_Username] UNIQUE ([Username])
)
GO

CREATE NONCLUSTERED INDEX [IX_MH_Users_MerchantId] ON [dbo].[MH_Users] ([MerchantId])
GO
CREATE NONCLUSTERED INDEX [IX_MH_Users_Email] ON [dbo].[MH_Users] ([Email])
GO

-- =============================================
-- User Roles Table
-- =============================================

CREATE TABLE [dbo].[MH_Roles] (
    [RoleId] INT IDENTITY(1,1) NOT NULL,
    [RoleName] VARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_MH_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
)
GO

INSERT INTO [dbo].[MH_Roles] ([RoleName], [Description]) VALUES
('Admin', 'System Administrator'),
('InternalSupport', 'Internal Support Staff'),
('MerchantOwner', 'Merchant Account Owner'),
('MerchantUser', 'Standard Merchant User'),
('MerchantReadOnly', 'Read-Only Merchant Access')
GO

-- =============================================
-- Transaction Tables
-- =============================================

CREATE TABLE [dbo].[MH_Transactions] (
    [TransactionId] BIGINT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [ReferenceNumber] VARCHAR(50) NULL,
    [AuthorizationCode] VARCHAR(50) NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [RefundAmount] DECIMAL(18,2) NULL,
    [Fee] DECIMAL(18,2) NULL,
    [NetAmount] DECIMAL(18,2) NULL,
    [Currency] CHAR(3) NOT NULL DEFAULT 'USD',
    [Status] VARCHAR(20) NOT NULL,
    [CardType] VARCHAR(20) NULL,
    [Last4Digits] CHAR(4) NULL,
    [CardFingerprint] VARCHAR(64) NULL,
    [EntryMode] VARCHAR(20) NULL,
    [Description] NVARCHAR(500) NULL,
    [CustomerName] NVARCHAR(100) NULL,
    [CustomerEmail] NVARCHAR(200) NULL,
    [BatchNumber] VARCHAR(50) NULL,
    [TransactionDate] DATETIME2 NOT NULL,
    [SettlementDate] DATETIME2 NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [DeclineReason] VARCHAR(50) NULL,
    [ResponseCode] VARCHAR(20) NULL,
    [TerminalId] VARCHAR(50) NULL,
    [PayGateTransactionId] BIGINT NULL,
    [ModifiedDate] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    CONSTRAINT [PK_MH_Transactions] PRIMARY KEY CLUSTERED ([TransactionId] ASC),
    CONSTRAINT [FK_MH_Transactions_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId])
)
GO

CREATE NONCLUSTERED INDEX [IX_MH_Transactions_MerchantId_Date] ON [dbo].[MH_Transactions] ([MerchantId], [TransactionDate] DESC) INCLUDE ([Amount], [Status])
GO
CREATE NONCLUSTERED INDEX [IX_MH_Transactions_Status] ON [dbo].[MH_Transactions] ([Status]) INCLUDE ([MerchantId], [Amount])
GO
CREATE NONCLUSTERED INDEX [IX_MH_Transactions_CardFingerprint] ON [dbo].[MH_Transactions] ([CardFingerprint])
GO
CREATE NONCLUSTERED INDEX [IX_MH_Transactions_BatchNumber] ON [dbo].[MH_Transactions] ([BatchNumber])
GO

-- =============================================
-- Refunds Table
-- =============================================

CREATE TABLE [dbo].[MH_Refunds] (
    [RefundId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransactionId] BIGINT NOT NULL,
    [RefundAmount] DECIMAL(18,2) NOT NULL,
    [ReferenceNumber] VARCHAR(50) NOT NULL,
    [Reason] NVARCHAR(500) NULL,
    [ProcessedBy] NVARCHAR(100) NOT NULL,
    [ProcessedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_MH_Refunds] PRIMARY KEY CLUSTERED ([RefundId] ASC),
    CONSTRAINT [FK_MH_Refunds_MH_Transactions] FOREIGN KEY ([TransactionId]) REFERENCES [dbo].[MH_Transactions] ([TransactionId])
)
GO

-- =============================================
-- Dispute Tables
-- =============================================

CREATE TABLE [dbo].[MH_Disputes] (
    [DisputeId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [TransactionId] BIGINT NOT NULL,
    [CaseNumber] VARCHAR(50) NULL,
    [ReasonCode] VARCHAR(50) NULL,
    [ReasonDescription] NVARCHAR(200) NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Status] VARCHAR(20) NOT NULL DEFAULT 'Open',
    [CardType] VARCHAR(20) NULL,
    [Last4Digits] CHAR(4) NULL,
    [FiledDate] DATETIME2 NOT NULL,
    [ResponseDeadline] DATETIME2 NOT NULL,
    [RespondedDate] DATETIME2 NULL,
    [ResolvedDate] DATETIME2 NULL,
    [MerchantResponse] NVARCHAR(MAX) NULL,
    [Resolution] NVARCHAR(500) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    CONSTRAINT [PK_MH_Disputes] PRIMARY KEY CLUSTERED ([DisputeId] ASC),
    CONSTRAINT [FK_MH_Disputes_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId]),
    CONSTRAINT [FK_MH_Disputes_MH_Transactions] FOREIGN KEY ([TransactionId]) REFERENCES [dbo].[MH_Transactions] ([TransactionId])
)
GO

CREATE NONCLUSTERED INDEX [IX_MH_Disputes_MerchantId_Status] ON [dbo].[MH_Disputes] ([MerchantId], [Status])
GO

CREATE TABLE [dbo].[MH_DisputeHistory] (
    [HistoryId] INT IDENTITY(1,1) NOT NULL,
    [DisputeId] INT NOT NULL,
    [Action] VARCHAR(50) NOT NULL,
    [Details] NVARCHAR(1000) NULL,
    [PerformedBy] NVARCHAR(100) NOT NULL,
    [ActionDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_MH_DisputeHistory] PRIMARY KEY CLUSTERED ([HistoryId] ASC),
    CONSTRAINT [FK_MH_DisputeHistory_MH_Disputes] FOREIGN KEY ([DisputeId]) REFERENCES [dbo].[MH_Disputes] ([DisputeId])
)
GO

CREATE TABLE [dbo].[MH_DisputeDocuments] (
    [DocumentId] INT IDENTITY(1,1) NOT NULL,
    [DisputeId] INT NOT NULL,
    [FileName] NVARCHAR(256) NOT NULL,
    [FilePath] NVARCHAR(500) NOT NULL,
    [DocumentType] VARCHAR(50) NULL,
    [FileSize] BIGINT NULL,
    [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [UploadedBy] NVARCHAR(100) NULL,
    CONSTRAINT [PK_MH_DisputeDocuments] PRIMARY KEY CLUSTERED ([DocumentId] ASC),
    CONSTRAINT [FK_MH_DisputeDocuments_MH_Disputes] FOREIGN KEY ([DisputeId]) REFERENCES [dbo].[MH_Disputes] ([DisputeId])
)
GO

-- =============================================
-- Report & Statement Tables
-- =============================================

CREATE TABLE [dbo].[MH_MonthlyStatements] (
    [StatementId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [Year] INT NOT NULL,
    [Month] INT NOT NULL,
    [StatementDate] DATETIME2 NOT NULL,
    [TotalVolume] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TotalTransactions] INT NOT NULL DEFAULT 0,
    [TotalFees] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [NetSettlement] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [ChargebackAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [ChargebackCount] INT NOT NULL DEFAULT 0,
    [RefundAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [RefundCount] INT NOT NULL DEFAULT 0,
    [FilePath] NVARCHAR(500) NULL,
    [GeneratedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_MH_MonthlyStatements] PRIMARY KEY CLUSTERED ([StatementId] ASC),
    CONSTRAINT [FK_MH_MonthlyStatements_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId]),
    CONSTRAINT [UQ_MH_MonthlyStatements] UNIQUE ([MerchantId], [Year], [Month])
)
GO

CREATE TABLE [dbo].[MH_GeneratedReports] (
    [GeneratedReportId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [ReportType] VARCHAR(50) NOT NULL,
    [FileName] NVARCHAR(256) NOT NULL,
    [FilePath] NVARCHAR(500) NULL,
    [FileSize] BIGINT NULL,
    [Format] VARCHAR(10) NOT NULL DEFAULT 'PDF',
    [GeneratedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [StartDate] DATETIME2 NULL,
    [EndDate] DATETIME2 NULL,
    [GeneratedBy] NVARCHAR(100) NULL,
    [Status] VARCHAR(20) NOT NULL DEFAULT 'Completed',
    [ErrorMessage] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_MH_GeneratedReports] PRIMARY KEY CLUSTERED ([GeneratedReportId] ASC)
)
GO

-- =============================================
-- Audit & API Tables
-- =============================================

CREATE TABLE [dbo].[MH_AuditLog] (
    [AuditId] BIGINT IDENTITY(1,1) NOT NULL,
    [EntityType] VARCHAR(50) NOT NULL,
    [EntityId] INT NOT NULL,
    [Action] VARCHAR(50) NOT NULL,
    [UserId] NVARCHAR(100) NOT NULL,
    [Details] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [IPAddress] VARCHAR(45) NULL,
    CONSTRAINT [PK_MH_AuditLog] PRIMARY KEY CLUSTERED ([AuditId] ASC)
)
GO

CREATE NONCLUSTERED INDEX [IX_MH_AuditLog_Entity] ON [dbo].[MH_AuditLog] ([EntityType], [EntityId])
GO
CREATE NONCLUSTERED INDEX [IX_MH_AuditLog_Date] ON [dbo].[MH_AuditLog] ([CreatedDate] DESC)
GO

CREATE TABLE [dbo].[MH_ApiKeys] (
    [KeyId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [KeyName] NVARCHAR(100) NOT NULL,
    [ApiKey] VARCHAR(64) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [LastUsedDate] DATETIME2 NULL,
    [UseCount] INT NULL DEFAULT 0,
    CONSTRAINT [PK_MH_ApiKeys] PRIMARY KEY CLUSTERED ([KeyId] ASC),
    CONSTRAINT [FK_MH_ApiKeys_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId]),
    CONSTRAINT [UQ_MH_ApiKeys_ApiKey] UNIQUE ([ApiKey])
)
GO

CREATE TABLE [dbo].[MH_MerchantDocuments] (
    [DocumentId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [DocumentType] VARCHAR(50) NOT NULL,
    [FileName] NVARCHAR(256) NOT NULL,
    [FilePath] NVARCHAR(500) NOT NULL,
    [FileSize] INT NULL,
    [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_MH_MerchantDocuments] PRIMARY KEY CLUSTERED ([DocumentId] ASC),
    CONSTRAINT [FK_MH_MerchantDocuments_MH_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[MH_Merchants] ([MerchantId])
)
GO

CREATE TABLE [dbo].[MH_NotificationPreferences] (
    [PreferenceId] INT IDENTITY(1,1) NOT NULL,
    [MerchantId] INT NOT NULL,
    [NotificationType] VARCHAR(50) NOT NULL,
    [IsEnabled] BIT NOT NULL DEFAULT 1,
    [Channel] VARCHAR(20) NOT NULL DEFAULT 'Email',
    CONSTRAINT [PK_MH_NotificationPreferences] PRIMARY KEY CLUSTERED ([PreferenceId] ASC)
)
GO
