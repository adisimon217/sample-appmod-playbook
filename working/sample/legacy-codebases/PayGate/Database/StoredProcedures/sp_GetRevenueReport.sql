-- =============================================
-- sp_GetRevenueReport
-- Generates revenue report with daily breakdown.
-- Uses windowing functions for cumulative totals and 7-day moving average.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetRevenueReport]
    @StartDate  DATE,
    @EndDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH DailyRevenue AS (
        SELECT 
            CAST(t.CreatedDate AS DATE) AS ReportDate,
            COUNT(*) AS TransactionCount,
            SUM(t.Amount) AS TotalAmount,
            SUM(ISNULL(t.ProcessingFee, 0)) AS TotalFees,
            SUM(t.Amount) - SUM(ISNULL(t.ProcessingFee, 0)) AS NetRevenue,
            SUM(CASE WHEN t.Status IN (1,2,7) THEN 1 ELSE 0 END) AS SuccessCount,
            SUM(CASE WHEN t.Status IN (3,4) THEN 1 ELSE 0 END) AS FailedCount,
            AVG(t.LatencyMs) AS AvgLatencyMs
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE CAST(t.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate
          AND t.TransactionType IN (1, 2, 3) -- Auth, Capture, Sale (not refunds)
        GROUP BY CAST(t.CreatedDate AS DATE)
    )
    SELECT 
        dr.ReportDate,
        dr.TransactionCount,
        dr.TotalAmount,
        dr.TotalFees,
        dr.NetRevenue,
        dr.SuccessCount,
        dr.FailedCount,
        dr.AvgLatencyMs,
        SUM(dr.TotalAmount) OVER (ORDER BY dr.ReportDate ROWS UNBOUNDED PRECEDING) AS CumulativeAmount,
        AVG(dr.TotalAmount) OVER (ORDER BY dr.ReportDate ROWS BETWEEN 6 PRECEDING AND CURRENT ROW) AS MovingAvg7Day,
        CAST(dr.SuccessCount AS FLOAT) / NULLIF(dr.TransactionCount, 0) * 100 AS SuccessRatePercent
    FROM DailyRevenue dr
    ORDER BY dr.ReportDate;
END
GO
