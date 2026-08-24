-- Inserts a new batch run record when reconciliation starts
CREATE PROCEDURE [dbo].[sp_InsertBatchRun]
    @BatchRunId         UNIQUEIDENTIFIER,
    @StartTime          DATETIME2(7),
    @Status             VARCHAR(50),
    @CorrelationId      VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[BatchRuns] (BatchRunId, StartTime, Status, CorrelationId)
    VALUES (@BatchRunId, @StartTime, @Status, @CorrelationId);
END
GO
