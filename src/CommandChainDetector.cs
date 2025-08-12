namespace port;

/// <summary>
/// Detects if the current command is part of a command chain by analyzing command execution context.
/// This helps determine when to suppress intermediate output like listing current state.
/// </summary>
internal class CommandChainDetector : ICommandChainDetector
{
    private readonly Lazy<bool> _shouldDisplayOutput;

    public CommandChainDetector()
    {
        _shouldDisplayOutput = new Lazy<bool>(DetectShouldDisplayOutput);
    }

    public bool ShouldDisplayOutput() => _shouldDisplayOutput.Value;

    private static bool DetectShouldDisplayOutput()
    {
        try
        {
            // Get the current command being executed
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 2)
                return true;

            var command = args[1].ToLowerInvariant();

            // Commands that typically end chains and should show output by default
            var finalCommands = new[] { "prune", "pr", "list", "ls" };

            // Commands that are typically intermediate in chains
            var intermediateCommands = new[] { "pull", "p", "run", "r" };

            // Simple heuristic: if this is a command that typically ends chains, show output
            // Otherwise, for intermediate commands, we suppress by default (conservative approach)
            if (finalCommands.Contains(command))
                return true;

            if (intermediateCommands.Contains(command))
                return false;

            // For other commands (commit, reset, stop, remove, etc.), show output by default
            return true;
        }
        catch
        {
            // If we can't determine the chain status, default to showing output
            return true;
        }
    }
}
