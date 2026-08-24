CREATE PROCEDURE sp_UpdateDisputeResolution
    @DisputeId INT,
    @Resolution VARCHAR(50),
    @Notes NVARCHAR(MAX),
    @ResolvedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Update the dispute
        UPDATE Disputes
        SET Status = 'Resolved',
            Resolution = @Resolution,
            Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 
                    CONVERT(VARCHAR(20), GETDATE(), 120) + ' - ' + @ResolvedBy + ': ' + @Notes,
            ResolvedBy = @ResolvedBy,
            ResolvedDate = GETDATE()
        WHERE DisputeId = @DisputeId;

        -- Insert into dispute history
        INSERT INTO DisputeHistory (DisputeId, Action, PerformedBy, ActionDate, Notes)
        VALUES (@DisputeId, 'Resolved', @ResolvedBy, GETDATE(), 
                'Resolution: ' + @Resolution + '. Notes: ' + @Notes);

        -- If refund, update transaction status
        IF @Resolution IN ('RefundFull', 'RefundPartial')
        BEGIN
            UPDATE t
            SET t.Status = 'Refunded'
            FROM Transactions t
            INNER JOIN Disputes d ON t.TransactionId = d.TransactionId
            WHERE d.DisputeId = @DisputeId;
        END

        -- Log audit trail
        INSERT INTO AuditTrail (Action, EntityType, EntityId, PerformedBy, Details)
        VALUES ('RESOLVE_DISPUTE', 'Dispute', @DisputeId, @ResolvedBy, 
                'Resolution: ' + @Resolution);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
