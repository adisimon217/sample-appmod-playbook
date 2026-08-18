-- Inserts a detected discrepancy record
CREATE PROCEDURE [dbo].[sp_InsertDiscrepancy]
    @DiscrepancyId          UNIQUEIDENTIFIER,
    @BatchRunId             UNIQUEIDENTIFIER,
    @Type                   VARCHAR(50),
    @SettlementRecordId     UNIQUEIDENTIFIER,
    @TransactionId          UNIQUEIDENTIFIER,
    @SettlementAmount       DECIMAL(18, 4),
    @TransactionAmount      DECIMAL(18, 4),
    @DiscrepancyAmount      DECIMAL(18, 4),
    @DetectedAt             DATETIME2(7),
    @Description            NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Discrepancies]
        (DiscrepancyId, BatchRunId, Type, SettlementRecordId, TransactionId,
         SettlementAmount, TransactionAmount, DiscrepancyAmount, DetectedAt, Description)
    VALUES
        (@DiscrepancyId, @BatchRunId, @Type, @SettlementRecordId, @TransactionId,
         @SettlementAmount, @TransactionAmount, @DiscrepancyAmount, @DetectedAt, @Description);
END
GO
