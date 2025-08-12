namespace port;

/// <summary>
/// Service to detect if the current command is being executed as part of a command chain.
/// This helps determine when to suppress intermediate output like listing current state.
/// </summary>
public interface ICommandChainDetector
{
    /// <summary>
    /// Determines if the current process is likely the last command in a command chain.
    /// Returns true if this command should display output, false if it should be suppressed.
    /// </summary>
    bool ShouldDisplayOutput();
}
