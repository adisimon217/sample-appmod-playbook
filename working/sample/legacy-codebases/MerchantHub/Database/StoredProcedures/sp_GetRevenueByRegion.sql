-- =============================================
-- sp_GetRevenueByRegion
-- Aggregates revenue by merchant state/region
-- Used for RevenueByRegion Crystal Report
-- Created: 2020-02-15 by J. Rodriguez
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetRevenueByRegion]
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        m.[State],
        CASE 
            WHEN m.[State] IN ('CT','ME','MA','NH','RI','VT') THEN 'New England'
            WHEN m.[State] IN ('NJ','NY','PA') THEN 'Mid-Atlantic'
            WHEN m.[State] IN ('IL','IN','MI','OH','WI','IA','KS','MN','MO','NE','ND','SD') THEN 'Midwest'
            WHEN m.[State] IN ('DE','FL','GA','MD','NC','SC','VA','DC','WV','AL','KY','MS','TN','AR','LA','OK','TX') THEN 'South'
            WHEN m.[State] IN ('AZ','CO','ID','MT','NV','NM','UT','WY') THEN 'Mountain'
            WHEN m.[State] IN ('AK','CA','HI','OR','WA') THEN 'Pacific'
            ELSE 'Other'
        END as Region,
        COUNT(DISTINCT m.MerchantId) as MerchantCount,
        COUNT(t.TransactionId) as TransactionCount,
        SUM(t.Amount) as TotalVolume,
        SUM(ISNULL(t.Fee, 0)) as TotalFees,
        AVG(t.Amount) as AvgTransactionAmount
    FROM MH_Merchants m
    INNER JOIN MH_Transactions t ON m.MerchantId = t.MerchantId
    WHERE t.TransactionDate >= @StartDate
      AND t.TransactionDate < @EndDate
      AND t.Status = 'Approved'
      AND m.Status = 'Active'
    GROUP BY m.[State],
        CASE 
            WHEN m.[State] IN ('CT','ME','MA','NH','RI','VT') THEN 'New England'
            WHEN m.[State] IN ('NJ','NY','PA') THEN 'Mid-Atlantic'
            WHEN m.[State] IN ('IL','IN','MI','OH','WI','IA','KS','MN','MO','NE','ND','SD') THEN 'Midwest'
            WHEN m.[State] IN ('DE','FL','GA','MD','NC','SC','VA','DC','WV','AL','KY','MS','TN','AR','LA','OK','TX') THEN 'South'
            WHEN m.[State] IN ('AZ','CO','ID','MT','NV','NM','UT','WY') THEN 'Mountain'
            WHEN m.[State] IN ('AK','CA','HI','OR','WA') THEN 'Pacific'
            ELSE 'Other'
        END
    ORDER BY Region, m.[State];
END
GO
