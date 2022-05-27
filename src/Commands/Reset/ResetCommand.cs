using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetCommand : AsyncCommand<ResetSettings>
{
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;

    public ResetCommand(IGetRunningContainerQuery getRunningContainerQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand)
    {
        _getRunningContainerQuery = getRunningContainerQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var container = await _getRunningContainerQuery.QueryAsync();
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}",
                _ => _stopAndRemoveContainerCommand.ExecuteAsync(container.Id));

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}",
                _ => _createContainerCommand.ExecuteAsync(container));

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}",
                _ => _runContainerCommand.ExecuteAsync(container));
        
        AnsiConsole.WriteLine($"Currently running container {container.Identifier} resetted");

        return 0;
    }
}