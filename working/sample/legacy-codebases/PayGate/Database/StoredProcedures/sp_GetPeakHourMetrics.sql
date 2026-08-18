-- =============================================
-- sp_GetPeakHourMetrics
-- Returns peak hour transaction metrics for capacity monitoring.
-- Uses windowing functions to calculate percentile latencies.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetPeakHourMetrics]
    @Date   DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH HourlyMetrics AS (
        SELECT 
            DATEPART(HOUR, CreatedDate) AS HourOfDay,
            COUNT(*) AS HourlyVolume,
            AVG(CAST(LatencyMs AS FLOAT)) AS AvgLatencyMs
        FROM dbo.Transactions WITH (NOLOCK)
        WHERE CAST(CreatedDate AS DATE) = @Date
        GROUP BY DATEPART(HOUR, CreatedDate)
    ),
    PeakHour AS (
        SELECT TOP 1
            HourOfDay AS PeakHour,
            HourlyVolume AS PeakVolume
        FROM HourlyMetrics
        ORDER BY HourlyVolume DESC
    ),
    LatencyPercentile AS (
        SELECT 
            PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY LatencyMs) OVER () AS P99LatencyMs
        FROM dbo.Transactions WITH (NOLOCK)
        WHERE CAST(CreatedDate AS DATE) = @Date
          AND LatencyMs IS NOT NULL
    )
    SELECT 
        ph.PeakVolume,
        ph.PeakHour,
        (SELECT COUNT(*) FROM dbo.Transactions WITH (NOLOCK) WHERE CAST(CreatedDate AS DATE) = @Date) AS TotalVolume,
        (SELECT AVG(CAST(LatencyMs AS FLOAT)) FROM dbo.Transactions WITH (NOLOCK) 
         WHERE CAST(CreatedDate AS DATE) = @Date AND LatencyMs IS NOT NULL) AS AvgLatencyMs,
        (SELECT TOP 1 P99LatencyMs FROM LatencyPercentile) AS P99LatencyMs
    FROM PeakHour ph;
END
GO
