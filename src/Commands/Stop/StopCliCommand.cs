using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Stop;

internal class StopCliCommand : AsyncCommand<StopSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IContainerNamePrompt _containerNamePrompt;
    private readonly IStopContainerCommand _stopContainerCommand;

    public StopCliCommand(IGetRunningContainersQuery getRunningContainersQuery,
        IContainerNamePrompt containerNamePrompt,
        IStopContainerCommand stopContainerCommand)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _containerNamePrompt = containerNamePrompt;
        _stopContainerCommand = stopContainerCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, StopSettings settings)
    {
        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await StopContainerAsync(container);

        return 0;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "stop");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }


    private async Task StopContainerAsync(Container container)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Stopping container '{container.ContainerName}'",
                async _ => { await _stopContainerCommand.ExecuteAsync(container.Id); });
        AnsiConsole.WriteLine(
            $"Currently running container '{container.ContainerName}' stopped");
    }
}