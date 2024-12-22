using port.Commands.List;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetCliCommand(
    IGetRunningContainersQuery getRunningContainersQuery,
    IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
    ICreateContainerCommand createContainerCommand,
    IRunContainerCommand runContainerCommand,
    IContainerNamePrompt containerNamePrompt,
    ListCliCommand listCliCommand)
    : AsyncCommand<ResetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await ResetContainerAsync(container);

        await listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await Spinner.StartAsync("Getting running containers",
            async _ => await getRunningContainersQuery.QueryAsync().ToListAsync());

        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }


    private async Task ResetContainerAsync(Container container)
    {
        await Spinner.StartAsync(
            $"Resetting container '{container.ContainerName}'",
            async _ =>
            {
                await stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                await createContainerCommand.ExecuteAsync(container);
                await runContainerCommand.ExecuteAsync(container);
            });
    }
}