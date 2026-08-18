-- =============================================
-- sp_ValidateApiKey
-- Validates an API key and returns associated merchant information.
-- Called on every API request - must be fast.
-- =============================================
CREATE PROCEDURE [dbo].[sp_ValidateApiKey]
    @ApiKey VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CASE 
            WHEN ak.ApiKeyId IS NOT NULL 
                 AND ak.IsActive = 1 
                 AND (ak.ExpiresDate IS NULL OR ak.ExpiresDate > SYSUTCDATETIME())
            THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS IsValid,
        ak.ApiKeyId,
        ak.MerchantId,
        CASE WHEN m.Status = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS MerchantActive,
        m.AllowedIpAddresses
    FROM dbo.ApiKeys ak WITH (NOLOCK)
    LEFT JOIN dbo.Merchants m WITH (NOLOCK) ON ak.MerchantId = m.MerchantId
    WHERE ak.ApiKey = @ApiKey;
    
    -- Update last used timestamp (fire and forget)
    UPDATE dbo.ApiKeys
    SET LastUsedDate = SYSUTCDATETIME()
    WHERE ApiKey = @ApiKey AND IsActive = 1;
END
GO
