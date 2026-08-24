-- Gets the match history (all retry attempts) for a specific reconciliation result
CREATE PROCEDURE [dbo].[sp_GetMatchHistory]
    @MatchResultId      UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT HistoryId, MatchResultId, AttemptNumber, Status, 
           AttemptTime, ErrorMessage, DurationMs
    FROM [dbo].[MatchHistory]
    WHERE MatchResultId = @MatchResultId
    ORDER BY AttemptNumber ASC;
END
GO
