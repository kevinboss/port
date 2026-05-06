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
    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var containerName = await ResolveContainerNameAsync(settings);
        await resetOrchestrator.WithRenderingAsync(o => o.ExecuteAsync(containerName));
        await listCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<string> ResolveContainerNameAsync(IContainerIdentifierSettings settings)
    {
        if (settings.ContainerIdentifier != null)
            return settings.ContainerIdentifier;

        var containers = await getRunningContainersQuery.QueryAsync().ToListAsync();
        if (containers.Count == 1)
            return containers.Single().ContainerName;

        return containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
    }
}
