-- =============================================
-- sp_GenerateMonthlyStatement
-- Generates monthly financial statement using PIVOT for card type breakdown
-- Created: 2018-06-01 by S. Patel
-- Modified: 2023-01-20 by D. Kim - added chargeback metrics
-- =============================================
CREATE PROCEDURE [dbo].[sp_GenerateMonthlyStatement]
    @MerchantId INT,
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartDate DATETIME2 = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate DATETIME2 = DATEADD(MONTH, 1, @StartDate);

    -- Check if statement already exists
    IF EXISTS (SELECT 1 FROM MH_MonthlyStatements WHERE MerchantId = @MerchantId AND [Year] = @Year AND [Month] = @Month)
    BEGIN
        RAISERROR('Monthly statement already exists for this period.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        -- PIVOT query to get card type breakdown
        ;WITH CardTypeBreakdown AS (
            SELECT 
                [Visa], [Mastercard], [Amex], [Discover]
            FROM (
                SELECT CardType, Amount
                FROM MH_Transactions WITH (NOLOCK)
                WHERE MerchantId = @MerchantId
                  AND TransactionDate >= @StartDate
                  AND TransactionDate < @EndDate
                  AND Status = 'Approved'
            ) AS src
            PIVOT (
                SUM(Amount)
                FOR CardType IN ([Visa], [Mastercard], [Amex], [Discover])
            ) AS pvt
        ),
        StatusBreakdown AS (
            SELECT
                SUM(CASE WHEN Status = 'Approved' THEN Amount ELSE 0 END) as ApprovedVolume,
                SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) as ApprovedCount,
                SUM(CASE WHEN Status = 'Declined' THEN 1 ELSE 0 END) as DeclinedCount,
                SUM(CASE WHEN Status = 'Refunded' THEN Amount ELSE 0 END) as RefundVolume,
                SUM(CASE WHEN Status = 'Refunded' THEN 1 ELSE 0 END) as RefundCount,
                SUM(CASE WHEN Status = 'Chargeback' THEN Amount ELSE 0 END) as ChargebackVolume,
                SUM(CASE WHEN Status = 'Chargeback' THEN 1 ELSE 0 END) as ChargebackCount,
                SUM(ISNULL(Fee, 0)) as TotalFees,
                COUNT(*) as TotalTransactions
            FROM MH_Transactions WITH (NOLOCK)
            WHERE MerchantId = @MerchantId
              AND TransactionDate >= @StartDate
              AND TransactionDate < @EndDate
        )
        INSERT INTO MH_MonthlyStatements (
            MerchantId, [Year], [Month], StatementDate,
            TotalVolume, TotalTransactions, TotalFees, NetSettlement,
            ChargebackAmount, ChargebackCount, RefundAmount, RefundCount,
            GeneratedDate
        )
        SELECT 
            @MerchantId,
            @Year,
            @Month,
            @StartDate,
            s.ApprovedVolume,
            s.ApprovedCount,
            s.TotalFees,
            s.ApprovedVolume - s.TotalFees - s.ChargebackVolume - s.RefundVolume,
            s.ChargebackVolume,
            s.ChargebackCount,
            s.RefundVolume,
            s.RefundCount,
            GETDATE()
        FROM StatusBreakdown s;

        -- Log the generation
        INSERT INTO MH_AuditLog (EntityType, EntityId, Action, UserId, Details, CreatedDate)
        VALUES ('MonthlyStatement', @MerchantId, 'Generated', 'SYSTEM',
                'Monthly statement generated for ' + CAST(@Year AS VARCHAR) + '/' + CAST(@Month AS VARCHAR),
                GETDATE());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
