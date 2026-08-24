-- =============================================
-- sp_GetTransactionsByMerchant
-- Retrieves paginated transactions for a merchant with aggregation.
-- Uses OFFSET/FETCH for pagination and windowing for running totals.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetTransactionsByMerchant]
    @MerchantId     VARCHAR(15),
    @StartDate      DATETIME2(3),
    @EndDate        DATETIME2(3),
    @PageNumber     INT = 1,
    @PageSize       INT = 100,
    @Status         INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Return paginated results with running total
    ;WITH FilteredTransactions AS (
        SELECT 
            t.TransactionId,
            t.Amount,
            t.Currency,
            t.TransactionType,
            t.Status,
            t.CreatedDate,
            t.ProcessedDate,
            t.AuthorizationCode,
            t.ResponseCode,
            t.CardBrand,
            t.CardLast4,
            t.ProcessingFee,
            t.LatencyMs,
            SUM(t.Amount) OVER (ORDER BY t.CreatedDate ROWS UNBOUNDED PRECEDING) AS RunningTotal,
            COUNT(*) OVER () AS TotalCount
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.MerchantId = @MerchantId
          AND t.CreatedDate >= @StartDate
          AND t.CreatedDate <= @EndDate
          AND (@Status IS NULL OR t.Status = @Status)
    )
    SELECT *
    FROM FilteredTransactions
    ORDER BY CreatedDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
