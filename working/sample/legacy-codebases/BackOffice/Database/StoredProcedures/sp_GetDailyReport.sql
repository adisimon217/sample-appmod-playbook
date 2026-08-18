CREATE PROCEDURE sp_GetDailyReport
    @DateFrom DATE,
    @DateTo DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CONVERT(VARCHAR(10), t.TransactionDate, 120) AS ReportDate,
        COUNT(*) AS TransactionCount,
        SUM(t.Amount) AS TotalAmount,
        AVG(t.Amount) AS AvgAmount,
        SUM(CASE WHEN t.Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
        SUM(CASE WHEN t.Status = 'Declined' THEN 1 ELSE 0 END) AS DeclinedCount,
        SUM(CASE WHEN t.IsFlagged = 1 THEN 1 ELSE 0 END) AS FlaggedCount,
        COUNT(DISTINCT t.MerchantId) AS UniqueMerchants
    FROM Transactions t
    WHERE CAST(t.TransactionDate AS DATE) BETWEEN @DateFrom AND @DateTo
    GROUP BY CONVERT(VARCHAR(10), t.TransactionDate, 120)
    ORDER BY ReportDate;
END
GO
