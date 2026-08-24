-- =============================================
-- PayGate - PaymentsDB Table Creation Script
-- SQL Server 2019 Enterprise Edition
-- TDE Enabled, Always On Availability Group
-- Database: PaymentsDB (850 GB)
-- =============================================

USE [PaymentsDB]
GO

-- =============================================
-- Transactions table (primary table, ~500M rows)
-- Partitioned by CreatedDate (monthly partitions)
-- =============================================
CREATE TABLE [dbo].[Transactions] (
    [TransactionId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [MerchantId]            VARCHAR(15)      NOT NULL,
    [Amount]                DECIMAL(18,2)    NOT NULL,
    [Currency]              VARCHAR(3)       NOT NULL DEFAULT 'USD',
    [CardNumber]            VARCHAR(19)      NOT NULL, -- Masked: 411111XXXXXX1234
    [CardExpiry]            VARCHAR(7)       NULL,
    [CardholderName]        NVARCHAR(100)    NULL,
    [TransactionType]       INT              NOT NULL, -- 1=Auth, 2=Capture, 3=Sale, 4=Refund, 5=Void, 6=Reversal
    [Status]                INT              NOT NULL DEFAULT 0, -- 0=Pending, 1=Auth, 2=Captured, 3=Declined, 4=Failed, 5=Voided, 6=Refunded, 7=Settled, 8=Chargeback
    [CreatedDate]           DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [ProcessedDate]         DATETIME2(3)     NULL,
    [SettledDate]           DATETIME2(3)     NULL,
    [AuthorizationCode]     VARCHAR(50)      NULL,
    [ResponseCode]          VARCHAR(10)      NULL,
    [ResponseMessage]       NVARCHAR(500)    NULL,
    [IpAddress]             VARCHAR(45)      NULL,
    [UserAgent]             NVARCHAR(500)    NULL,
    [BatchId]               UNIQUEIDENTIFIER NULL,
    [OriginalTransactionId] UNIQUEIDENTIFIER NULL,
    [ProcessingFee]         DECIMAL(18,2)    NULL,
    [CardBrand]             VARCHAR(50)      NULL,
    [CardBin]               VARCHAR(6)       NULL,
    [CardLast4]             VARCHAR(4)       NULL,
    [LatencyMs]             INT              NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED ([TransactionId] ASC)
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, FILLFACTOR = 90)
) ON [PRIMARY]
GO

-- Indexes for common query patterns
CREATE NONCLUSTERED INDEX [IX_Transactions_MerchantId_CreatedDate]
    ON [dbo].[Transactions] ([MerchantId], [CreatedDate] DESC)
    INCLUDE ([Amount], [Status], [TransactionType], [CardBrand])
GO

CREATE NONCLUSTERED INDEX [IX_Transactions_CardNumber_CreatedDate]
    ON [dbo].[Transactions] ([CardNumber], [CreatedDate] DESC)
    INCLUDE ([MerchantId], [Amount], [Status])
GO

CREATE NONCLUSTERED INDEX [IX_Transactions_Status_ProcessedDate]
    ON [dbo].[Transactions] ([Status], [ProcessedDate])
    INCLUDE ([MerchantId], [Amount], [CardBrand])
    WHERE [Status] IN (2, 7) -- Captured, Settled
GO

CREATE NONCLUSTERED INDEX [IX_Transactions_BatchId]
    ON [dbo].[Transactions] ([BatchId])
    WHERE [BatchId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_Transactions_CreatedDate_Status]
    ON [dbo].[Transactions] ([CreatedDate], [Status])
    INCLUDE ([MerchantId], [Amount], [LatencyMs])
GO

-- =============================================
-- Settlements table
-- =============================================
CREATE TABLE [dbo].[Settlements] (
    [SettlementId]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [BatchId]               UNIQUEIDENTIFIER NOT NULL,
    [Network]               VARCHAR(20)      NOT NULL, -- VISA, MASTERCARD
    [SettlementDate]        DATE             NOT NULL,
    [TotalAmount]           DECIMAL(18,2)    NOT NULL,
    [TransactionCount]      INT              NOT NULL,
    [Status]                INT              NOT NULL DEFAULT 0,
    [FileName]              VARCHAR(100)     NULL,
    [FileGeneratedDate]     DATETIME2(3)     NULL,
    [FileSentDate]          DATETIME2(3)     NULL,
    [ResponseReceivedDate]  DATETIME2(3)     NULL,
    [ErrorMessage]          NVARCHAR(1000)   NULL,
    [ChargebackAmount]      DECIMAL(18,2)    NULL,
    [ChargebackCount]       INT              NULL,
    [NetSettlementAmount]   DECIMAL(18,2)    NULL,
    CONSTRAINT [PK_Settlements] PRIMARY KEY CLUSTERED ([SettlementId] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Settlements_SettlementDate_Network]
    ON [dbo].[Settlements] ([SettlementDate], [Network])
    INCLUDE ([Status], [TotalAmount])
GO

CREATE NONCLUSTERED INDEX [IX_Settlements_BatchId]
    ON [dbo].[Settlements] ([BatchId])
GO

-- =============================================
-- Merchants table
-- =============================================
CREATE TABLE [dbo].[Merchants] (
    [MerchantId]              VARCHAR(15)    NOT NULL,
    [BusinessName]            NVARCHAR(200)  NOT NULL,
    [TaxId]                   VARCHAR(20)    NULL,
    [ContactEmail]            NVARCHAR(200)  NULL,
    [ContactPhone]            VARCHAR(20)    NULL,
    [Status]                  INT            NOT NULL DEFAULT 0,
    [CreatedDate]             DATETIME2(3)   NOT NULL DEFAULT SYSUTCDATETIME(),
    [ActivatedDate]           DATETIME2(3)   NULL,
    [SuspendedDate]           DATETIME2(3)   NULL,
    [SuspensionReason]        NVARCHAR(500)  NULL,
    [SuspendedBy]             NVARCHAR(100)  NULL,
    [MccCode]                 VARCHAR(10)    NULL,
    [SettlementSchedule]      VARCHAR(20)    NULL DEFAULT 'T+1',
    [MaxTransactionsPerHour]  INT            NOT NULL DEFAULT 1000,
    [WebhookUrl]              NVARCHAR(500)  NULL,
    [AllowedIpAddresses]      NVARCHAR(2000) NULL,
    [ProcessingFeePercent]    DECIMAL(5,4)   NULL,
    [MonthlyMinimumFee]       DECIMAL(18,2)  NULL,
    [RiskCategory]            VARCHAR(50)    NULL,
    CONSTRAINT [PK_Merchants] PRIMARY KEY CLUSTERED ([MerchantId] ASC)
) ON [PRIMARY]
GO

-- =============================================
-- PaymentMethods table (tokenized cards)
-- =============================================
CREATE TABLE [dbo].[PaymentMethods] (
    [PaymentMethodId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [MerchantId]       VARCHAR(15)      NOT NULL,
    [CustomerId]       VARCHAR(100)     NULL,
    [Token]            VARCHAR(50)      NOT NULL,
    [CardBrand]        VARCHAR(20)      NULL,
    [CardBin]          VARCHAR(6)       NULL,
    [CardLast4]        VARCHAR(4)       NULL,
    [ExpiryDate]       VARCHAR(7)       NULL,
    [CardholderName]   NVARCHAR(100)    NULL,
    [IsDefault]        BIT              NOT NULL DEFAULT 0,
    [IsActive]         BIT              NOT NULL DEFAULT 1,
    [CreatedDate]      DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [LastUsedDate]     DATETIME2(3)     NULL,
    [IssuingBank]      VARCHAR(50)      NULL,
    [IssuingCountry]   VARCHAR(2)       NULL,
    CONSTRAINT [PK_PaymentMethods] PRIMARY KEY CLUSTERED ([PaymentMethodId] ASC)
) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_PaymentMethods_Token]
    ON [dbo].[PaymentMethods] ([Token])
GO

-- =============================================
-- AuditLog table
-- =============================================
CREATE TABLE [dbo].[AuditLog] (
    [AuditId]       BIGINT IDENTITY(1,1) NOT NULL,
    [TableName]     VARCHAR(100)    NOT NULL,
    [RecordId]      VARCHAR(100)    NOT NULL,
    [Action]        VARCHAR(20)     NOT NULL, -- INSERT, UPDATE, DELETE
    [ChangedBy]     NVARCHAR(100)   NULL,
    [ChangedDate]   DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [OldValues]     NVARCHAR(MAX)   NULL,
    [NewValues]     NVARCHAR(MAX)   NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([AuditId] ASC)
) ON [PRIMARY]
GO

-- =============================================
-- ApiKeys table
-- =============================================
CREATE TABLE [dbo].[ApiKeys] (
    [ApiKeyId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [MerchantId]    VARCHAR(15)      NOT NULL,
    [ApiKey]        VARCHAR(64)      NOT NULL, -- SHA-256 hash of the actual key
    [KeyPrefix]     VARCHAR(8)       NOT NULL, -- First 8 chars for identification
    [IsActive]      BIT              NOT NULL DEFAULT 1,
    [CreatedDate]   DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [ExpiresDate]   DATETIME2(3)     NULL,
    [LastUsedDate]  DATETIME2(3)     NULL,
    [Description]   NVARCHAR(200)    NULL,
    CONSTRAINT [PK_ApiKeys] PRIMARY KEY CLUSTERED ([ApiKeyId] ASC),
    CONSTRAINT [FK_ApiKeys_Merchants] FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[Merchants]([MerchantId])
) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiKeys_ApiKey]
    ON [dbo].[ApiKeys] ([ApiKey])
    WHERE [IsActive] = 1
GO

-- =============================================
-- Foreign Key Constraints
-- =============================================
ALTER TABLE [dbo].[Transactions]
    ADD CONSTRAINT [FK_Transactions_Merchants]
    FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[Merchants]([MerchantId])
GO

-- =============================================
-- Audit Trigger on Merchants table
-- =============================================
CREATE TRIGGER [dbo].[trg_Merchants_Audit]
ON [dbo].[Merchants]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[AuditLog] ([TableName], [RecordId], [Action], [ChangedBy], [OldValues], [NewValues])
    SELECT 
        'Merchants',
        i.MerchantId,
        'UPDATE',
        SUSER_SNAME(),
        (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM inserted i
    INNER JOIN deleted d ON i.MerchantId = d.MerchantId;
END
GO
