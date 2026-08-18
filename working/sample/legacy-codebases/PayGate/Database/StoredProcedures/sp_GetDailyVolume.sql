-- =============================================
-- sp_GetDailyVolume
-- Returns daily transaction volume with hourly breakdown.
-- Uses PIVOT to transform hourly data into columns.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetDailyVolume]
    @Date       DATE,
    @MerchantId VARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Hourly volume breakdown using PIVOT
    SELECT *
    FROM (
        SELECT 
            DATEPART(HOUR, CreatedDate) AS HourOfDay,
            Amount
        FROM dbo.Transactions WITH (NOLOCK)
        WHERE CAST(CreatedDate AS DATE) = @Date
          AND (@MerchantId IS NULL OR MerchantId = @MerchantId)
    ) AS SourceData
    PIVOT (
        COUNT(Amount)
        FOR HourOfDay IN (
            [0],[1],[2],[3],[4],[5],[6],[7],[8],[9],[10],[11],
            [12],[13],[14],[15],[16],[17],[18],[19],[20],[21],[22],[23]
        )
    ) AS PivotTable;
    
    -- Summary statistics
    SELECT 
        COUNT(*) AS TotalTransactions,
        SUM(Amount) AS TotalAmount,
        AVG(Amount) AS AvgAmount,
        MAX(Amount) AS MaxAmount,
        SUM(CASE WHEN Status IN (1,2,7) THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN Status IN (3,4) THEN 1 ELSE 0 END) AS FailedCount,
        CAST(SUM(CASE WHEN Status IN (1,2,7) THEN 1.0 ELSE 0 END) / 
             NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS SuccessRate
    FROM dbo.Transactions WITH (NOLOCK)
    WHERE CAST(CreatedDate AS DATE) = @Date
      AND (@MerchantId IS NULL OR MerchantId = @MerchantId);
END
GO
