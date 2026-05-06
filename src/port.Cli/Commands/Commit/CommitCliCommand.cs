using port.Commands.List;
using port.Orchestrators;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

public class CommitCliCommand(
    IGetRunningContainersQuery getRunningContainersQuery,
    IContainerNamePrompt containerNamePrompt,
    ICommitOrchestrator commitOrchestrator,
    ListCliCommand listCliCommand
) : AsyncCommand<CommitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var containerName = await ResolveContainerNameAsync(settings);
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";
        await commitOrchestrator.WithRenderingAsync(o =>
            o.ExecuteAsync(containerName, tag, settings.Overwrite, settings.Switch)
        );
        await listCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<string> ResolveContainerNameAsync(IContainerIdentifierSettings settings)
    {
        if (settings.ContainerIdentifier != null)
            return settings.ContainerIdentifier;

        var containers = await getRunningContainersQuery.QueryAsync().ToListAsync();
        return containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "commit");
    }
}
