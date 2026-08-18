-- ============================================================================
-- ComplianceReporter - Linked Server Setup Script
-- ============================================================================
-- Purpose: Creates the Linked Server connection from SQLPROD01 (ComplianceReporting)
--          to PAYGATE_SERVER (PayGateDB) for read-only compliance reporting access.
--
-- Execution: Run on SQLPROD01 by DBA with sysadmin rights
-- Last Updated: 2019-03-15 (added ComplianceAlerts table access)
-- DBA Contact: John Tan, ext. 4521
--
-- IMPORTANT: The service account svc_compliance@voyager.local only needs
--            db_datareader on PayGateDB. Do NOT grant write permissions.
-- ============================================================================

-- Step 1: Create the Linked Server
IF NOT EXISTS (SELECT * FROM sys.servers WHERE name = 'PAYGATE_SERVER')
BEGIN
    EXEC sp_addlinkedserver
        @server = N'PAYGATE_SERVER',
        @srvproduct = N'',
        @provider = N'SQLNCLI',
        @datasrc = N'SQLPROD02\PAYGATE';

    PRINT 'Linked Server PAYGATE_SERVER created successfully.';
END
ELSE
BEGIN
    PRINT 'Linked Server PAYGATE_SERVER already exists.';
END
GO

-- Step 2: Configure security mapping for svc_compliance
EXEC sp_addlinkedsrvlogin
    @rmtsrvname = N'PAYGATE_SERVER',
    @useself = N'FALSE',
    @locallogin = N'voyager\svc_compliance',
    @rmtuser = N'voyager\svc_compliance',
    @rmtpassword = NULL;  -- Uses Windows auth (pass-through)
GO

-- Step 3: Configure Linked Server options
EXEC sp_serveroption
    @server = N'PAYGATE_SERVER',
    @optname = N'data access',
    @optvalue = N'TRUE';

EXEC sp_serveroption
    @server = N'PAYGATE_SERVER',
    @optname = N'rpc',
    @optvalue = N'TRUE';

EXEC sp_serveroption
    @server = N'PAYGATE_SERVER',
    @optname = N'rpc out',
    @optvalue = N'TRUE';

-- Set query timeout for linked server queries (compliance reports can be slow)
EXEC sp_serveroption
    @server = N'PAYGATE_SERVER',
    @optname = N'query timeout',
    @optvalue = N'600';  -- 10 minutes

EXEC sp_serveroption
    @server = N'PAYGATE_SERVER',
    @optname = N'connect timeout',
    @optvalue = N'30';
GO

-- Step 4: Verify connectivity
DECLARE @result INT;
EXEC @result = sp_testlinkedserver @servername = N'PAYGATE_SERVER';

IF @result = 0
    PRINT 'Linked Server PAYGATE_SERVER connectivity verified.';
ELSE
    PRINT 'WARNING: Linked Server PAYGATE_SERVER connectivity test FAILED!';
GO

-- Step 5: Verify access to required tables
SELECT TOP 1 'Transactions' AS TableName, COUNT(*) AS RowCount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Transactions]
UNION ALL
SELECT 'Merchants', COUNT(*)
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Merchants]
UNION ALL
SELECT 'ComplianceAlerts', COUNT(*)
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[ComplianceAlerts]
UNION ALL
SELECT 'KycRecords', COUNT(*)
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[KycRecords]
UNION ALL
SELECT 'SuspiciousTransactionReports', COUNT(*)
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[SuspiciousTransactionReports];
GO

PRINT 'Linked Server setup complete. ComplianceReporter service can now access PayGateDB.';
GO
