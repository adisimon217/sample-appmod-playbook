namespace ReconcEngine.Models.Enums
{
    /// <summary>
    /// Type of reconciliation discrepancy detected.
    /// </summary>
    public enum DiscrepancyType
    {
        AmountMismatch,
        MissingTransaction,
        MissingSettlement,
        DuplicateSettlement,
        DuplicateTransaction,
        CurrencyMismatch,
        DateMismatch
    }
}
