-- Updates batch run status and counts when reconciliation completes or fails
CREATE PROCEDURE [dbo].[sp_UpdateBatchStatus]
    @BatchRunId         UNIQUEIDENTIFIER,
    @Status             VARCHAR(50),
    @EndTime            DATETIME2(7),
    @MatchedCount       INT,
    @DiscrepancyCount   INT,
    @FailedCount        INT,
    @ErrorMessage       NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[BatchRuns]
    SET Status = @Status,
        EndTime = @EndTime,
        MatchedCount = @MatchedCount,
        DiscrepancyCount = @DiscrepancyCount,
        FailedCount = @FailedCount,
        ErrorMessage = @ErrorMessage
    WHERE BatchRunId = @BatchRunId;
END
GO
