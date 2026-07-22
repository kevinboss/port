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
    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings, CancellationToken cancellationToken)
    {
        var containerName = await ResolveContainerNameAsync(settings, cancellationToken);
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";
        await commitOrchestrator.WithRenderingAsync(o =>
            o.ExecuteAsync(containerName, tag, settings.Overwrite, settings.Switch, cancellationToken)
        );
        await listCliCommand.ExecuteAsync(cancellationToken);
        return 0;
    }

    private async Task<string> ResolveContainerNameAsync(IContainerIdentifierSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ContainerIdentifier != null)
            return settings.ContainerIdentifier;

        var containers = await getRunningContainersQuery.QueryAsync().ToListAsync(cancellationToken);
        return containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "commit");
    }
}
