-- =============================================
-- sp_GetMerchantTransactions
-- Gets paginated transactions for a merchant with filtering
-- Created: 2018-02-10 by D. Kim
-- Modified: 2022-11-15 by J. Rodriguez - added NOLOCK hints for performance
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetMerchantTransactions]
    @MerchantId INT,
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @Status VARCHAR(20) = NULL,
    @CardType VARCHAR(20) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @SortColumn VARCHAR(50) = 'TransactionDate',
    @SortDirection VARCHAR(4) = 'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Get total count for pagination
    SELECT COUNT(*) as TotalCount
    FROM [dbo].[MH_Transactions] WITH (NOLOCK)
    WHERE MerchantId = @MerchantId
      AND TransactionDate >= @StartDate
      AND TransactionDate < @EndDate
      AND (@Status IS NULL OR Status = @Status)
      AND (@CardType IS NULL OR CardType = @CardType)
      AND (@SearchTerm IS NULL OR 
           ReferenceNumber LIKE '%' + @SearchTerm + '%' OR
           CustomerName LIKE '%' + @SearchTerm + '%' OR
           Last4Digits LIKE '%' + @SearchTerm + '%');

    -- Get paginated results
    SELECT 
        t.TransactionId,
        t.ReferenceNumber,
        t.AuthorizationCode,
        t.Amount,
        t.RefundAmount,
        t.Fee,
        t.NetAmount,
        t.Currency,
        t.Status,
        t.CardType,
        t.Last4Digits,
        t.EntryMode,
        t.Description,
        t.CustomerName,
        t.CustomerEmail,
        t.BatchNumber,
        t.TransactionDate,
        t.SettlementDate,
        t.DeclineReason,
        t.ResponseCode,
        t.TerminalId
    FROM [dbo].[MH_Transactions] t WITH (NOLOCK)
    WHERE t.MerchantId = @MerchantId
      AND t.TransactionDate >= @StartDate
      AND t.TransactionDate < @EndDate
      AND (@Status IS NULL OR t.Status = @Status)
      AND (@CardType IS NULL OR t.CardType = @CardType)
      AND (@SearchTerm IS NULL OR 
           t.ReferenceNumber LIKE '%' + @SearchTerm + '%' OR
           t.CustomerName LIKE '%' + @SearchTerm + '%' OR
           t.Last4Digits LIKE '%' + @SearchTerm + '%')
    ORDER BY 
        CASE WHEN @SortColumn = 'TransactionDate' AND @SortDirection = 'DESC' THEN t.TransactionDate END DESC,
        CASE WHEN @SortColumn = 'TransactionDate' AND @SortDirection = 'ASC' THEN t.TransactionDate END ASC,
        CASE WHEN @SortColumn = 'Amount' AND @SortDirection = 'DESC' THEN t.Amount END DESC,
        CASE WHEN @SortColumn = 'Amount' AND @SortDirection = 'ASC' THEN t.Amount END ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
