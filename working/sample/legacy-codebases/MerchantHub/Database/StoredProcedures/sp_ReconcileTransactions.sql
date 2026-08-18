-- =============================================
-- sp_ReconcileTransactions
-- Reconciles MerchantHub transactions with PayGate records
-- Created: 2019-08-01 by D. Kim
-- Modified: 2022-03-15 - added mismatch logging
-- =============================================
CREATE PROCEDURE [dbo].[sp_ReconcileTransactions]
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @AutoFix BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Find transactions in MerchantHub that don't have matching PayGate records
    SELECT 
        t.TransactionId,
        t.MerchantId,
        t.Amount as MH_Amount,
        t.Status as MH_Status,
        t.PayGateTransactionId,
        pg.Amount as PG_Amount,
        pg.Status as PG_Status,
        CASE 
            WHEN pg.PayGateTransactionId IS NULL THEN 'Missing in PayGate'
            WHEN t.Amount != pg.Amount THEN 'Amount Mismatch'
            WHEN t.Status != pg.Status THEN 'Status Mismatch'
            ELSE 'OK'
        END as ReconciliationStatus
    FROM MH_Transactions t WITH (NOLOCK)
    LEFT JOIN [PayGateDB].[dbo].[PG_Transactions] pg 
        ON t.PayGateTransactionId = pg.PayGateTransactionId
    WHERE t.TransactionDate >= @StartDate
      AND t.TransactionDate < @EndDate
      AND t.PayGateTransactionId IS NOT NULL
      AND (pg.PayGateTransactionId IS NULL 
           OR t.Amount != pg.Amount 
           OR t.Status != pg.Status)
    ORDER BY t.TransactionDate DESC;

    -- Summary counts
    SELECT 
        COUNT(*) as TotalChecked,
        SUM(CASE WHEN pg.PayGateTransactionId IS NULL THEN 1 ELSE 0 END) as MissingInPayGate,
        SUM(CASE WHEN pg.PayGateTransactionId IS NOT NULL AND t.Amount != pg.Amount THEN 1 ELSE 0 END) as AmountMismatches,
        SUM(CASE WHEN pg.PayGateTransactionId IS NOT NULL AND t.Amount = pg.Amount AND t.Status != pg.Status THEN 1 ELSE 0 END) as StatusMismatches
    FROM MH_Transactions t WITH (NOLOCK)
    LEFT JOIN [PayGateDB].[dbo].[PG_Transactions] pg 
        ON t.PayGateTransactionId = pg.PayGateTransactionId
    WHERE t.TransactionDate >= @StartDate
      AND t.TransactionDate < @EndDate
      AND t.PayGateTransactionId IS NOT NULL;
END
GO
