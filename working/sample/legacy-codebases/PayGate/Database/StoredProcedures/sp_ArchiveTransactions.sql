-- =============================================
-- sp_ArchiveTransactions
-- Archives transactions older than the cutoff date to archive table.
-- Processes in batches to avoid lock escalation on the main table.
-- Retention policy: 7 years active, then archive.
-- =============================================
CREATE PROCEDURE [dbo].[sp_ArchiveTransactions]
    @CutoffDate DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @BatchSize INT = 10000;
    DECLARE @TotalArchived INT = 0;
    DECLARE @CurrentBatch INT = 1;
    DECLARE @RowsAffected INT = 1;
    
    -- Create archive table if not exists
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transactions_Archive')
    BEGIN
        SELECT TOP 0 * INTO dbo.Transactions_Archive FROM dbo.Transactions;
        CREATE CLUSTERED INDEX IX_Archive_CreatedDate ON dbo.Transactions_Archive(CreatedDate);
    END
    
    WHILE @RowsAffected > 0
    BEGIN
        BEGIN TRANSACTION;
        
        -- Move batch to archive
        ;WITH CTE AS (
            SELECT TOP (@BatchSize) *
            FROM dbo.Transactions
            WHERE CreatedDate < @CutoffDate
              AND Status IN (7, 5, 6, 3, 4) -- Only archive terminal states
            ORDER BY CreatedDate ASC
        )
        INSERT INTO dbo.Transactions_Archive
        SELECT * FROM CTE;
        
        SET @RowsAffected = @@ROWCOUNT;
        
        -- Delete archived records from main table
        ;WITH CTE AS (
            SELECT TOP (@BatchSize) TransactionId
            FROM dbo.Transactions
            WHERE CreatedDate < @CutoffDate
              AND Status IN (7, 5, 6, 3, 4)
            ORDER BY CreatedDate ASC
        )
        DELETE FROM dbo.Transactions
        WHERE TransactionId IN (SELECT TransactionId FROM CTE);
        
        SET @TotalArchived = @TotalArchived + @RowsAffected;
        
        COMMIT TRANSACTION;
        
        -- Brief pause to reduce I/O pressure
        WAITFOR DELAY '00:00:01';
        
        SET @CurrentBatch = @CurrentBatch + 1;
    END
    
    -- Log the archive operation
    INSERT INTO dbo.AuditLog (TableName, RecordId, Action, ChangedBy, NewValues)
    VALUES (
        'Transactions',
        'ARCHIVE_' + CONVERT(VARCHAR(36), NEWID()),
        'ARCHIVE',
        SUSER_SNAME(),
        '{"CutoffDate":"' + CONVERT(VARCHAR, @CutoffDate, 126) + '","TotalArchived":' + CAST(@TotalArchived AS VARCHAR) + '}'
    );
    
    -- Return the archived count via RETURN value
    RETURN @TotalArchived;
END
GO
