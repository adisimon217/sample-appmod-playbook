-- Gets all discrepancies for a specific batch run
CREATE PROCEDURE [dbo].[sp_GetDiscrepancies]
    @BatchRunId     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DiscrepancyId, BatchRunId, Type, SettlementRecordId, TransactionId,
           SettlementAmount, TransactionAmount, DiscrepancyAmount,
           DetectedAt, Description, IsResolved, ResolvedAt, ResolvedBy
    FROM [dbo].[Discrepancies]
    WHERE BatchRunId = @BatchRunId
    ORDER BY ABS(DiscrepancyAmount) DESC;
END
GO
