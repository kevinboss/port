using Docker.DotNet.Models;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class CommitCommand : AsyncCommand<CommitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var containerToCommit = await DockerClientFacade.GetRunningContainersAsync();

        if (containerToCommit == null)
        {
            throw new InvalidOperationException();
        }

        await DockerClientFacade.CreateImageFromContainerAsync(containerToCommit, settings.Identifier);

        return 0;
    }
}