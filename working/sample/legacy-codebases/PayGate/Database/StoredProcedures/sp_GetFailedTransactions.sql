-- =============================================
-- sp_GetFailedTransactions
-- Returns failed transactions for monitoring and retry.
-- Uses windowing functions to identify patterns (repeat failures).
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetFailedTransactions]
    @Date           DATE,
    @MerchantId     VARCHAR(15) = NULL,
    @TopN           INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH FailedTxns AS (
        SELECT 
            t.TransactionId,
            t.MerchantId,
            m.BusinessName,
            t.Amount,
            t.CardBrand,
            t.CardLast4,
            t.ResponseCode,
            t.ResponseMessage,
            t.CreatedDate,
            t.IpAddress,
            t.LatencyMs,
            COUNT(*) OVER (PARTITION BY t.MerchantId) AS MerchantFailCount,
            COUNT(*) OVER (PARTITION BY t.CardLast4) AS CardFailCount,
            COUNT(*) OVER (PARTITION BY t.ResponseCode) AS ResponseCodeCount,
            ROW_NUMBER() OVER (ORDER BY t.CreatedDate DESC) AS RowNum
        FROM dbo.Transactions t WITH (NOLOCK)
        INNER JOIN dbo.Merchants m WITH (NOLOCK) ON t.MerchantId = m.MerchantId
        WHERE CAST(t.CreatedDate AS DATE) = @Date
          AND t.Status IN (3, 4) -- Declined, Failed
          AND (@MerchantId IS NULL OR t.MerchantId = @MerchantId)
    )
    SELECT TOP (@TopN) *
    FROM FailedTxns
    ORDER BY CreatedDate DESC;
    
    -- Summary by response code
    SELECT 
        ResponseCode,
        COUNT(*) AS FailCount,
        SUM(Amount) AS TotalAmount,
        MIN(CreatedDate) AS FirstOccurrence,
        MAX(CreatedDate) AS LastOccurrence
    FROM dbo.Transactions WITH (NOLOCK)
    WHERE CAST(CreatedDate AS DATE) = @Date
      AND Status IN (3, 4)
      AND (@MerchantId IS NULL OR MerchantId = @MerchantId)
    GROUP BY ResponseCode
    ORDER BY FailCount DESC;
END
GO
