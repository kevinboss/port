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
            .StartAsync(
                $"Resetting container {ContainerNameHelper.JoinContainerNameAndTag(container.ContainerName, container.ContainerTag)}",
                async _ =>
                {
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                    await _createContainerCommand.ExecuteAsync(container);
                    await _runContainerCommand.ExecuteAsync(container);
                });
        AnsiConsole.WriteLine(
            $"Currently running container {ContainerNameHelper.JoinContainerNameAndTag(container.ContainerName, container.ContainerTag)} resetted");

        return 0;
    }
}