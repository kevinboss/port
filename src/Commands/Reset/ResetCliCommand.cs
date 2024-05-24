using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetCliCommand : AsyncCommand<ResetSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IContainerNamePrompt _containerNamePrompt;
    private readonly ListCliCommand _listCliCommand;

    public ResetCliCommand(IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand, IContainerNamePrompt containerNamePrompt,
        ListCliCommand listCliCommand)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _containerNamePrompt = containerNamePrompt;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await ResetContainerAsync(container);

        await _listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync().ToListAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }


    private async Task ResetContainerAsync(Container container)
    {
        await Spinner.StartAsync(
                $"Resetting container '{container.ContainerName}'",
                async _ =>
                {
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                    await _createContainerCommand.ExecuteAsync(container);
                    await _runContainerCommand.ExecuteAsync(container);
                });
    }
}