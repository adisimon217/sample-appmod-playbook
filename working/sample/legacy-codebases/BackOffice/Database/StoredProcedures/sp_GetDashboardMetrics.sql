CREATE PROCEDURE sp_GetDashboardMetrics
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        (SELECT COUNT(*) FROM Transactions WHERE CAST(TransactionDate AS DATE) = @Date) AS TotalTransactions,
        (SELECT ISNULL(SUM(Amount), 0) FROM Transactions WHERE CAST(TransactionDate AS DATE) = @Date) AS TotalAmount,
        (SELECT COUNT(*) FROM Disputes WHERE Status NOT IN ('Closed', 'Resolved')) AS OpenDisputes,
        (SELECT COUNT(*) FROM Disputes WHERE Status = 'Escalated') AS EscalatedDisputes,
        (SELECT COUNT(*) FROM Merchants WHERE Status = 'Pending') AS PendingMerchants,
        (SELECT COUNT(DISTINCT UserName) FROM AuditLog 
         WHERE CAST(RequestTime AS DATE) = @Date AND UserName != 'Anonymous') AS ActiveUsers;
END
GO
