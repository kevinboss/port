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

    public ResetCliCommand(IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand,
        IContainerNamePrompt containerNamePrompt)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _containerNamePrompt = containerNamePrompt;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await ResetContainerAsync(container);

        return 0;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.Name == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
        return containers.SingleOrDefault(c => c.Name == identifier);
    }


    private async Task ResetContainerAsync(Container container)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Resetting container '{container.Name}'",
                async _ =>
                {
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                    await _createContainerCommand.ExecuteAsync(container);
                    await _runContainerCommand.ExecuteAsync(container);
                });
        AnsiConsole.WriteLine(
            $"Currently running container '{container.Name}' resetted");
    }
}