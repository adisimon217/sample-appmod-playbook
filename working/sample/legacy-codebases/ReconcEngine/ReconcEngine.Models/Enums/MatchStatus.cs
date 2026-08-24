namespace ReconcEngine.Models.Enums
{
    /// <summary>
    /// Status of a reconciliation match attempt.
    /// </summary>
    public enum MatchStatus
    {
        Matched,
        MatchedWithDiscrepancy,
        Unmatched,
        PendingRetry,
        PermanentlyFailed
    }
}
