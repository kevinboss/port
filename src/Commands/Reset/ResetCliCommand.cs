using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetCliCommand : AsyncCommand<ResetSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IContainerIdentifierAndTagEvaluator _containerIdentifierAndTagEvaluator;
    private readonly IIdentifierPrompt _identifierPrompt;

    public ResetCliCommand(IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand, IContainerIdentifierAndTagEvaluator containerIdentifierAndTagEvaluator,
        IIdentifierPrompt identifierPrompt)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _containerIdentifierAndTagEvaluator = containerIdentifierAndTagEvaluator;
        _identifierPrompt = identifierPrompt;
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
            var (identifier, tag) = _containerIdentifierAndTagEvaluator.Evaluate(settings.ContainerIdentifier);
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
        else
        {
            var (identifier, tag) = _identifierPrompt.GetIdentifierOfContainerFromUser(containers, "reset");
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
    }


    private async Task ResetContainerAsync(Container container)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Resetting container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}",
                async _ =>
                {
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                    await _createContainerCommand.ExecuteAsync(container);
                    await _runContainerCommand.ExecuteAsync(container);
                });
        AnsiConsole.WriteLine(
            $"Currently running container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)} resetted");
    }
}