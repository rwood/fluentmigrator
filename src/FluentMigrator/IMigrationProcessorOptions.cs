namespace FluentMigrator
{
    /// <summary>
    /// Options for diagnosing SLQ script failures.
    /// </summary>
    public enum ScriptFailureAction
    {
        FailMigration,                      // Normal behaviour if an SQL script fails.  Rollsback transaction.
        LogFailureAndContinue,              // Create a log file for the failing script and continues (transaction not aborted).
        LogFailureAndStopExecutingScripts   // Create a log file for the failing script and stops processing any more scripts. Allow you to diagnose the failing script in this state.
    }

    public interface IMigrationProcessorOptions
    {
        bool PreviewOnly { get; }
        int Timeout { get; }
        string ProviderSwitches { get; }
        ScriptFailureAction ScriptFailureAction { get; }
    }
}