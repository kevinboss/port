using port.Commands.List;
using port.Orchestrators;
using Spectre.Console.Cli;

namespace port.Commands.Stop;

public class StopCliCommand : AsyncCommand<StopSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IContainerNamePrompt _containerNamePrompt;
    private readonly IStopOrchestrator _stopOrchestrator;
    private readonly ListCliCommand _listCliCommand;

    public StopCliCommand(
        IGetRunningContainersQuery getRunningContainersQuery,
        IContainerNamePrompt containerNamePrompt,
        IStopOrchestrator stopOrchestrator,
        ListCliCommand listCliCommand
    )
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _containerNamePrompt = containerNamePrompt;
        _stopOrchestrator = stopOrchestrator;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, StopSettings settings, CancellationToken cancellationToken)
    {
        var containerName = await ResolveContainerNameAsync(settings, cancellationToken);
        await _stopOrchestrator.WithRenderingAsync(o => o.ExecuteAsync(containerName, cancellationToken));
        await _listCliCommand.ExecuteAsync(cancellationToken);
        return 0;
    }

    private async Task<string> ResolveContainerNameAsync(IContainerIdentifierSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ContainerIdentifier != null)
            return settings.ContainerIdentifier;

        var containers = await _getRunningContainersQuery.QueryAsync().ToListAsync(cancellationToken);
        return _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "stop");
    }
}
