-- BackOffice Database Schema
-- BackOfficeDB on SQL Server 2016 Standard
-- ~120 GB database, ~150 stored procedures

USE BackOfficeDB;
GO

-- Merchants table
CREATE TABLE Merchants (
    MerchantId INT IDENTITY(1,1) PRIMARY KEY,
    MerchantName NVARCHAR(200) NOT NULL,
    LegalName NVARCHAR(300) NOT NULL,
    TaxId VARCHAR(20) NOT NULL,
    BusinessType VARCHAR(50) NOT NULL,
    MccCode VARCHAR(10),
    AnnualVolume DECIMAL(18,2),
    ContactName NVARCHAR(200),
    Email VARCHAR(200),
    Phone VARCHAR(30),
    Address NVARCHAR(500),
    City VARCHAR(100),
    State VARCHAR(50),
    Zip VARCHAR(20),
    BankName NVARCHAR(200),
    RoutingNumber VARCHAR(9),
    AccountNumber VARCHAR(20),
    AccountType VARCHAR(20),
    RiskTier VARCHAR(20) DEFAULT 'Low',
    ChargebackLimit DECIMAL(5,2) DEFAULT 1.50,
    RiskNotes NVARCHAR(MAX),
    CreditLimit DECIMAL(18,2) DEFAULT 0,
    CurrentBalance DECIMAL(18,2) DEFAULT 0,
    UnderReview BIT DEFAULT 0,
    ReviewReason NVARCHAR(500),
    RiskLevel VARCHAR(20) DEFAULT 'Low',
    Status VARCHAR(30) DEFAULT 'Pending',
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastModifiedBy NVARCHAR(100),
    LastModifiedDate DATETIME,
    CONSTRAINT UQ_Merchants_TaxId UNIQUE (TaxId)
);
GO

-- Transactions table
CREATE TABLE Transactions (
    TransactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    MerchantId INT NOT NULL FOREIGN KEY REFERENCES Merchants(MerchantId),
    CardNumber VARCHAR(20) NOT NULL, -- Stored masked after first/last 4
    CardType VARCHAR(30),
    Amount DECIMAL(18,2) NOT NULL,
    Currency VARCHAR(3) DEFAULT 'USD',
    TransactionDate DATETIME NOT NULL,
    ProcessedDate DATETIME,
    Status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    ResponseCode VARCHAR(10),
    AuthorizationCode VARCHAR(20),
    IsFlagged BIT DEFAULT 0,
    FlaggedBy NVARCHAR(100),
    FlagReason NVARCHAR(500),
    FlaggedDate DATETIME,
    Notes NVARCHAR(MAX),
    BatchId INT,
    TerminalId VARCHAR(50),
    EntryMode VARCHAR(20),
    INDEX IX_Transactions_Date NONCLUSTERED (TransactionDate),
    INDEX IX_Transactions_MerchantId NONCLUSTERED (MerchantId),
    INDEX IX_Transactions_Status NONCLUSTERED (Status),
    INDEX IX_Transactions_Flagged NONCLUSTERED (IsFlagged) WHERE IsFlagged = 1
);
GO

-- Disputes table
CREATE TABLE Disputes (
    DisputeId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionId BIGINT NOT NULL FOREIGN KEY REFERENCES Transactions(TransactionId),
    CardholderName NVARCHAR(200),
    DisputeReason NVARCHAR(500) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'New',
    AssignedTo NVARCHAR(100),
    Resolution VARCHAR(50),
    Notes NVARCHAR(MAX),
    EscalatedBy NVARCHAR(100),
    EscalatedDate DATETIME,
    ResolvedBy NVARCHAR(100),
    ResolvedDate DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE(),
    DaysOpen AS DATEDIFF(DAY, CreatedDate, GETDATE()),
    INDEX IX_Disputes_Status NONCLUSTERED (Status),
    INDEX IX_Disputes_TransactionId NONCLUSTERED (TransactionId)
);
GO

-- Chargebacks table
CREATE TABLE Chargebacks (
    ChargebackId INT IDENTITY(1,1) PRIMARY KEY,
    DisputeId INT FOREIGN KEY REFERENCES Disputes(DisputeId),
    TransactionId BIGINT NOT NULL FOREIGN KEY REFERENCES Transactions(TransactionId),
    Amount DECIMAL(18,2) NOT NULL,
    ChargebackDate DATETIME NOT NULL,
    ReasonCode VARCHAR(10) NOT NULL,
    IsWon BIT DEFAULT 0,
    ResponseDueDate DATETIME,
    ResponseSubmitted BIT DEFAULT 0,
    FraudType VARCHAR(50),
    Status VARCHAR(30) DEFAULT 'Open',
    INDEX IX_Chargebacks_Date NONCLUSTERED (ChargebackDate)
);
GO

-- Users table (operations staff)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    WindowsLogin NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email VARCHAR(200),
    Department VARCHAR(50),
    Role VARCHAR(50),
    IsActive BIT DEFAULT 1,
    LastLogin DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedBy NVARCHAR(100),
    ModifiedDate DATETIME,
    CONSTRAINT UQ_Users_WindowsLogin UNIQUE (WindowsLogin)
);
GO

-- User Permissions
CREATE TABLE UserPermissions (
    UserPermissionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PermissionName VARCHAR(50) NOT NULL,
    GrantedBy NVARCHAR(100),
    GrantedDate DATETIME DEFAULT GETDATE(),
    INDEX IX_UserPermissions_UserId NONCLUSTERED (UserId)
);
GO

-- Fraud Alerts
CREATE TABLE FraudAlerts (
    AlertId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionId BIGINT FOREIGN KEY REFERENCES Transactions(TransactionId),
    FraudType VARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2),
    AlertDate DATETIME DEFAULT GETDATE(),
    Severity VARCHAR(20),
    Status VARCHAR(30) DEFAULT 'Open',
    ReviewedBy NVARCHAR(100),
    ReviewedDate DATETIME
);
GO

-- Audit Log
CREATE TABLE AuditLog (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(100),
    HttpMethod VARCHAR(10),
    RequestUrl NVARCHAR(500),
    StatusCode INT,
    ClientIp VARCHAR(50),
    ElapsedMs BIGINT,
    RequestTime DATETIME DEFAULT GETDATE(),
    INDEX IX_AuditLog_RequestTime NONCLUSTERED (RequestTime),
    INDEX IX_AuditLog_UserName NONCLUSTERED (UserName)
);
GO

-- Audit Trail (business actions)
CREATE TABLE AuditTrail (
    AuditTrailId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(50),
    EntityId INT,
    PerformedBy NVARCHAR(100),
    Details NVARCHAR(MAX),
    ActionDate DATETIME DEFAULT GETDATE(),
    INDEX IX_AuditTrail_Action NONCLUSTERED (Action),
    INDEX IX_AuditTrail_Date NONCLUSTERED (ActionDate)
);
GO

-- Dispute History
CREATE TABLE DisputeHistory (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    DisputeId INT NOT NULL FOREIGN KEY REFERENCES Disputes(DisputeId),
    Action VARCHAR(100) NOT NULL,
    PerformedBy NVARCHAR(100),
    ActionDate DATETIME DEFAULT GETDATE(),
    Notes NVARCHAR(MAX),
    INDEX IX_DisputeHistory_DisputeId NONCLUSTERED (DisputeId)
);
GO
