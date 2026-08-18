-- =============================================
-- sp_BatchSettlement
-- Marks captured transactions as settled and creates settlement records.
-- Runs nightly as part of end-of-day processing.
-- Uses CROSS APPLY and windowing functions for batch aggregation.
-- =============================================
CREATE PROCEDURE [dbo].[sp_BatchSettlement]
    @SettlementDate     DATE,
    @Network            VARCHAR(20) = NULL -- NULL = all networks
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @BatchId UNIQUEIDENTIFIER = NEWID();
    DECLARE @SettledCount INT = 0;
    DECLARE @TotalAmount DECIMAL(18,2) = 0;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Create temporary table with settlement candidates
        ;WITH SettlementCandidates AS (
            SELECT 
                t.TransactionId,
                t.MerchantId,
                t.Amount,
                t.CardBrand,
                t.ProcessedDate,
                ROW_NUMBER() OVER (PARTITION BY t.CardBrand ORDER BY t.ProcessedDate) AS RowNum,
                SUM(t.Amount) OVER (PARTITION BY t.CardBrand) AS NetworkTotal,
                COUNT(*) OVER (PARTITION BY t.CardBrand) AS NetworkCount
            FROM dbo.Transactions t WITH (UPDLOCK, READPAST)
            WHERE t.Status = 2 -- Captured
              AND CAST(t.ProcessedDate AS DATE) = @SettlementDate
              AND (@Network IS NULL OR t.CardBrand = @Network)
              AND t.TransactionType IN (1, 2, 3) -- Auth, Capture, Sale
        )
        UPDATE t
        SET t.Status = 7, -- Settled
            t.SettledDate = SYSUTCDATETIME(),
            t.BatchId = @BatchId
        FROM dbo.Transactions t
        INNER JOIN SettlementCandidates sc ON t.TransactionId = sc.TransactionId;
        
        SET @SettledCount = @@ROWCOUNT;
        
        -- Calculate totals by network
        SELECT @TotalAmount = ISNULL(SUM(Amount), 0)
        FROM dbo.Transactions WITH (NOLOCK)
        WHERE BatchId = @BatchId;
        
        -- Create settlement records per network
        INSERT INTO dbo.Settlements (SettlementId, BatchId, Network, SettlementDate, TotalAmount, TransactionCount, Status)
        SELECT 
            NEWID(),
            @BatchId,
            CardBrand,
            @SettlementDate,
            SUM(Amount),
            COUNT(*),
            0 -- Pending
        FROM dbo.Transactions WITH (NOLOCK)
        WHERE BatchId = @BatchId
        GROUP BY CardBrand;
        
        COMMIT TRANSACTION;
        
        -- Return summary
        SELECT 
            @BatchId AS BatchId,
            @SettledCount AS SettledCount,
            @TotalAmount AS TotalAmount,
            @SettlementDate AS SettlementDate;
            
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
