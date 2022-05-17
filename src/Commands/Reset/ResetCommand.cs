using dcma.Commands.Run;
using Spectre.Console.Cli;

namespace dcma.Commands.Reset;

internal class ResetCommand : AsyncCommand<ResetSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;

    public ResetCommand(IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var container = await _getRunningContainersQuery.QueryAsync();
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);

        await _createContainerCommand.ExecuteAsync(container);

        await _runContainerCommand.ExecuteAsync(container.ContainerName);

        return 0;
    }
}