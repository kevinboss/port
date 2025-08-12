using port.Commands.List;

namespace port;

/// <summary>
/// Wrapper for ListCliCommand that respects command chaining detection.
/// When commands are chained together, only the last command should display the list output.
/// </summary>
internal class ConditionalListCliCommand
{
    private readonly ListCliCommand _listCliCommand;
    private readonly ICommandChainDetector _commandChainDetector;

    public ConditionalListCliCommand(
        ListCliCommand listCliCommand,
        ICommandChainDetector commandChainDetector
    )
    {
        _listCliCommand = listCliCommand;
        _commandChainDetector = commandChainDetector;
    }

    /// <summary>
    /// Executes the list command only if we should display output (i.e., not in the middle of a command chain).
    /// </summary>
    public async Task ExecuteAsync()
    {
        if (_commandChainDetector.ShouldDisplayOutput())
        {
            await _listCliCommand.ExecuteAsync();
        }
    }

    /// <summary>
    /// Executes the list command with settings only if we should display output.
    /// </summary>
    public async Task<int> ExecuteAsync(ListSettings settings)
    {
        if (_commandChainDetector.ShouldDisplayOutput())
        {
            return await _listCliCommand.ExecuteAsync(null!, settings);
        }
        return 0;
    }
}
