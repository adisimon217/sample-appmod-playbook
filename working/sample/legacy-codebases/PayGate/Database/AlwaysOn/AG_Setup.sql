-- =============================================
-- PayGate - Always On Availability Group Setup
-- SQL Server 2019 Enterprise Edition
-- 
-- Topology:
--   Primary: SQLPROD-AG-01 (Read-Write)
--   Secondary: SQLPROD-AG-02 (Read-Only, Synchronous commit)
--   Secondary: SQLPROD-DR-01 (Async, DR site)
--
-- Listener: SQLPROD-AG-LISTENER (port 1433)
-- Database: PaymentsDB (850 GB, TDE Enabled)
--
-- Prerequisites:
--   - Windows Server Failover Cluster (WSFC) configured
--   - All nodes running SQL Server 2019 Enterprise
--   - TDE certificate backed up and restored to secondaries
--   - Service accounts configured with proper permissions
-- =============================================

-- =============================================
-- Step 1: Enable Always On on each instance (requires restart)
-- Run on each node via SQL Server Configuration Manager
-- =============================================
-- ALTER SETTING: SQL Server Configuration Manager > SQL Server Services
-- > SQL Server (PAYGATE) > Properties > AlwaysOn High Availability tab
-- > Enable AlwaysOn Availability Groups = checked
-- Net-Stop / Net-Start MSSQL$PAYGATE

-- =============================================
-- Step 2: Create endpoint on each node
-- =============================================

-- Run on SQLPROD-AG-01 (Primary)
USE [master]
GO

CREATE ENDPOINT [Hadr_endpoint]
    STATE = STARTED
    AS TCP (LISTENER_PORT = 5022, LISTENER_IP = ALL)
    FOR DATA_MIRRORING (
        ROLE = ALL,
        AUTHENTICATION = WINDOWS NEGOTIATE,
        ENCRYPTION = REQUIRED ALGORITHM AES
    );
GO

GRANT CONNECT ON ENDPOINT::[Hadr_endpoint] TO [PAYGATE\svc_sqlserver];
GO

-- Run on SQLPROD-AG-02 (Secondary - sync)
-- Same CREATE ENDPOINT statement on secondary node

-- Run on SQLPROD-DR-01 (DR - async)
-- Same CREATE ENDPOINT statement on DR node

-- =============================================
-- Step 3: Create the Availability Group
-- Run on Primary (SQLPROD-AG-01)
-- =============================================

CREATE AVAILABILITY GROUP [PayGate_AG]
WITH (
    AUTOMATED_BACKUP_PREFERENCE = SECONDARY,
    DB_FAILOVER = ON,
    DTC_SUPPORT = NONE,
    REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = 1
)
FOR DATABASE [PaymentsDB]
REPLICA ON
    N'SQLPROD-AG-01' WITH (
        ENDPOINT_URL = N'TCP://SQLPROD-AG-01.paygate.internal:5022',
        AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
        FAILOVER_MODE = AUTOMATIC,
        SEEDING_MODE = AUTOMATIC,
        PRIMARY_ROLE (
            ALLOW_CONNECTIONS = ALL,
            READ_ONLY_ROUTING_LIST = (N'SQLPROD-AG-02', N'SQLPROD-DR-01')
        ),
        SECONDARY_ROLE (
            ALLOW_CONNECTIONS = READ_ONLY,
            READ_ONLY_ROUTING_URL = N'TCP://SQLPROD-AG-01.paygate.internal:1433'
        ),
        SESSION_TIMEOUT = 10
    ),
    N'SQLPROD-AG-02' WITH (
        ENDPOINT_URL = N'TCP://SQLPROD-AG-02.paygate.internal:5022',
        AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
        FAILOVER_MODE = AUTOMATIC,
        SEEDING_MODE = AUTOMATIC,
        PRIMARY_ROLE (
            ALLOW_CONNECTIONS = ALL,
            READ_ONLY_ROUTING_LIST = (N'SQLPROD-AG-01', N'SQLPROD-DR-01')
        ),
        SECONDARY_ROLE (
            ALLOW_CONNECTIONS = READ_ONLY,
            READ_ONLY_ROUTING_URL = N'TCP://SQLPROD-AG-02.paygate.internal:1433'
        ),
        SESSION_TIMEOUT = 10
    ),
    N'SQLPROD-DR-01' WITH (
        ENDPOINT_URL = N'TCP://SQLPROD-DR-01.paygate.internal:5022',
        AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT,
        FAILOVER_MODE = MANUAL,
        SEEDING_MODE = AUTOMATIC,
        PRIMARY_ROLE (
            ALLOW_CONNECTIONS = ALL
        ),
        SECONDARY_ROLE (
            ALLOW_CONNECTIONS = READ_ONLY,
            READ_ONLY_ROUTING_URL = N'TCP://SQLPROD-DR-01.paygate.internal:1433'
        ),
        SESSION_TIMEOUT = 30
    );
GO

-- =============================================
-- Step 4: Join secondary replicas
-- Run on SQLPROD-AG-02
-- =============================================
ALTER AVAILABILITY GROUP [PayGate_AG] JOIN;
GO
ALTER AVAILABILITY GROUP [PayGate_AG] GRANT CREATE ANY DATABASE;
GO

-- Run on SQLPROD-DR-01
-- Same JOIN statements

-- =============================================
-- Step 5: Create the AG Listener
-- Run on Primary
-- =============================================
ALTER AVAILABILITY GROUP [PayGate_AG]
ADD LISTENER N'SQLPROD-AG-LISTENER' (
    WITH IP (
        (N'10.1.10.100', N'255.255.255.0'),  -- Production subnet
        (N'10.2.10.100', N'255.255.255.0')   -- DR subnet
    ),
    PORT = 1433
);
GO

-- =============================================
-- Step 6: Configure Read-Only Routing
-- =============================================
ALTER AVAILABILITY GROUP [PayGate_AG]
MODIFY REPLICA ON N'SQLPROD-AG-01' WITH (
    PRIMARY_ROLE (READ_ONLY_ROUTING_LIST = (N'SQLPROD-AG-02', N'SQLPROD-DR-01'))
);
GO

ALTER AVAILABILITY GROUP [PayGate_AG]
MODIFY REPLICA ON N'SQLPROD-AG-02' WITH (
    PRIMARY_ROLE (READ_ONLY_ROUTING_LIST = (N'SQLPROD-AG-01', N'SQLPROD-DR-01'))
);
GO

-- =============================================
-- Step 7: Enable TDE on PaymentsDB
-- (Must be done before adding to AG, or certificate must be
--  backed up and restored to all replicas)
-- =============================================

-- Create master key (if not exists)
USE [master]
GO
CREATE MASTER KEY ENCRYPTION BY PASSWORD = '<StrongPassword>';
GO

-- Create certificate for TDE
CREATE CERTIFICATE PayGate_TDE_Cert
    WITH SUBJECT = 'PayGate TDE Certificate',
    EXPIRY_DATE = '2030-12-31';
GO

-- Backup certificate for secondary replicas
BACKUP CERTIFICATE PayGate_TDE_Cert
    TO FILE = 'C:\PayGate\Certs\PayGate_TDE_Cert.cer'
    WITH PRIVATE KEY (
        FILE = 'C:\PayGate\Certs\PayGate_TDE_Cert.pvk',
        ENCRYPTION BY PASSWORD = '<CertBackupPassword>'
    );
GO

-- Create database encryption key
USE [PaymentsDB]
GO
CREATE DATABASE ENCRYPTION KEY
    WITH ALGORITHM = AES_256
    ENCRYPTION BY SERVER CERTIFICATE PayGate_TDE_Cert;
GO

-- Enable TDE
ALTER DATABASE [PaymentsDB] SET ENCRYPTION ON;
GO

-- =============================================
-- Step 8: Verify AG health
-- =============================================
SELECT 
    ag.name AS ag_name,
    ars.role_desc,
    ar.replica_server_name,
    ars.synchronization_health_desc,
    ars.connected_state_desc,
    drs.database_id,
    drs.synchronization_state_desc,
    drs.log_send_queue_size,
    drs.redo_queue_size
FROM sys.dm_hadr_availability_replica_states ars
INNER JOIN sys.availability_replicas ar ON ars.replica_id = ar.replica_id
INNER JOIN sys.availability_groups ag ON ar.group_id = ag.group_id
LEFT JOIN sys.dm_hadr_database_replica_states drs ON ars.replica_id = drs.replica_id
ORDER BY ar.replica_server_name;
GO

-- =============================================
-- Step 9: Configure backup preferences
-- Backups run on secondary to offload primary
-- =============================================
ALTER AVAILABILITY GROUP [PayGate_AG]
MODIFY REPLICA ON N'SQLPROD-AG-02' WITH (
    BACKUP_PRIORITY = 50
);
GO

-- Full backup job (runs on preferred secondary)
-- BACKUP DATABASE [PaymentsDB] TO DISK = 'S:\Backups\PaymentsDB_Full.bak'
-- WITH COMPRESSION, CHECKSUM, INIT;

-- Log backup every 15 minutes
-- BACKUP LOG [PaymentsDB] TO DISK = 'S:\Backups\PaymentsDB_Log.trn'
-- WITH COMPRESSION, CHECKSUM;
