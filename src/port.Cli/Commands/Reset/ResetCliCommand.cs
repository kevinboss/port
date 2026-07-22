using port.Commands.List;
using port.Orchestrators;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

public class ResetCliCommand(
    IGetRunningContainersQuery getRunningContainersQuery,
    IContainerNamePrompt containerNamePrompt,
    IResetOrchestrator resetOrchestrator,
    ListCliCommand listCliCommand
) : AsyncCommand<ResetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings, CancellationToken cancellationToken)
    {
        var containerName = await ResolveContainerNameAsync(settings, cancellationToken);
        await resetOrchestrator.WithRenderingAsync(o => o.ExecuteAsync(containerName, cancellationToken));
        await listCliCommand.ExecuteAsync(cancellationToken);
        return 0;
    }

    private async Task<string> ResolveContainerNameAsync(IContainerIdentifierSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ContainerIdentifier != null)
            return settings.ContainerIdentifier;

        var containers = await getRunningContainersQuery.QueryAsync().ToListAsync(cancellationToken);
        if (containers.Count == 1)
            return containers.Single().ContainerName;

        return containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
    }
}
