-- =============================================
-- sp_GetMerchantDashboard
-- Returns all dashboard metrics in a single call
-- Created: 2019-05-10 by S. Patel
-- NOTE: This proc was created to reduce N+1 queries from the dashboard
--       controller but the controller still does its own queries too.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetMerchantDashboard]
    @MerchantId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
    DECLARE @StartOfWeek DATE = DATEADD(DAY, -(DATEPART(WEEKDAY, @Today) - 1), @Today);

    -- Monthly metrics
    SELECT 
        SUM(Amount) as MonthlyVolume,
        COUNT(*) as MonthlyCount,
        SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) as ApprovedCount,
        SUM(CASE WHEN Status = 'Declined' THEN 1 ELSE 0 END) as DeclinedCount,
        SUM(CASE WHEN Status = 'Chargeback' THEN 1 ELSE 0 END) as ChargebackCount,
        CASE WHEN COUNT(*) > 0 
             THEN CAST(SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*) * 100
             ELSE 0 END as ApprovalRate,
        CASE WHEN COUNT(*) > 0
             THEN CAST(SUM(CASE WHEN Status = 'Chargeback' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*) * 100
             ELSE 0 END as ChargebackRatio
    FROM MH_Transactions WITH (NOLOCK)
    WHERE MerchantId = @MerchantId
      AND TransactionDate >= @StartOfMonth;

    -- Today's metrics
    SELECT 
        ISNULL(SUM(Amount), 0) as TodayVolume,
        COUNT(*) as TodayCount
    FROM MH_Transactions WITH (NOLOCK)
    WHERE MerchantId = @MerchantId
      AND TransactionDate >= @Today;

    -- Open disputes
    SELECT 
        COUNT(*) as OpenDisputeCount,
        ISNULL(SUM(Amount), 0) as DisputeAmount
    FROM MH_Disputes WITH (NOLOCK)
    WHERE MerchantId = @MerchantId
      AND Status IN ('Open', 'UnderReview');

    -- Recent transactions
    SELECT TOP 10
        TransactionId, TransactionDate, Amount, Status, CardType, Last4Digits, Description
    FROM MH_Transactions WITH (NOLOCK)
    WHERE MerchantId = @MerchantId
    ORDER BY TransactionDate DESC;
END
GO
