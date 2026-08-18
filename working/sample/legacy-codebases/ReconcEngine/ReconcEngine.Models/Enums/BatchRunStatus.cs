namespace ReconcEngine.Models.Enums
{
    /// <summary>
    /// Status of a batch reconciliation run.
    /// </summary>
    public enum BatchRunStatus
    {
        Running,
        Completed,
        CompletedNoData,
        Failed,
        Cancelled
    }
}
