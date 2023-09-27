using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Run;

internal class RunCliCommand : AsyncCommand<RunSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly ICreateImageCliChildCommand _createImageCliChildCommand;
    private readonly IDoesImageExistQuery _doesImageExistQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ListCliCommand _listCliCommand;

    private const char PortSeparator = ':';

    public RunCliCommand(IImageIdentifierPrompt imageIdentifierPrompt,
        ICreateImageCliChildCommand createImageCliChildCommand, IDoesImageExistQuery doesImageExistQuery,
        IGetContainersQuery getContainersQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        IStopContainerCommand stopContainerCommand, Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, ListCliCommand listCliCommand)
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _createImageCliChildCommand = createImageCliChildCommand;
        _doesImageExistQuery = doesImageExistQuery;
        _getContainersQuery = getContainersQuery;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _stopContainerCommand = stopContainerCommand;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not launch untagged image");
        await TerminateOtherContainersAsync(identifier, tag);
        await LaunchImageAsync(identifier, tag, settings.Reset);

        await _listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _imageIdentifierPrompt.GetRunnableIdentifierAndTagFromUserAsync("run");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private Task TerminateOtherContainersAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var hostPorts = imageConfig.Ports
            .Select(e => e.Split(PortSeparator)[0])
            .ToList();
        var spinnerTex = $"Terminating containers using host ports '{string.Join(", ", hostPorts)}'";
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(spinnerTex, async _ =>
            {
                var containers = GetRunningContainersUsingHostPortsAsync(hostPorts);
                await foreach (var container in containers)
                    await _stopContainerCommand.ExecuteAsync(container.Id);
            });
    }

    private IAsyncEnumerable<Container> GetRunningContainersUsingHostPortsAsync(
        IEnumerable<string> hostPorts)
    {
        return _getContainersQuery.QueryRunningAsync()
            .Where(container =>
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (container.PortBindings is null) return false;
                var usedHostPorts = container.PortBindings
                    .SelectMany(pb => pb.Value
                        .Select(hp => hp.HostPort));
                return container.PortBindings
                    .Any(p => { return hostPorts.Any(p => usedHostPorts.Contains(p)); });
            });
    }

    private async Task LaunchImageAsync(string identifier, string? tag, bool resetContainer)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var ports = imageConfig.Ports;
        var environment = imageConfig.Environment;
        if (!await _doesImageExistQuery.QueryAsync(imageName, tag))
            await _createImageCliChildCommand.ExecuteAsync(imageName, tag);

        var containerName = ContainerNameHelper.BuildContainerName(identifier, tag);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {ImageNameHelper.BuildImageName(identifier, tag)}", async _ =>
            {
                var containers = await _getContainersQuery.QueryByContainerNameAsync(containerName).ToListAsync();
                switch (containers.Count)
                {
                    case 1 when resetContainer:
                        await _stopAndRemoveContainerCommand.ExecuteAsync(containers.Single().Id);
                        containerName =
                            await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports, environment);
                        break;
                    case 1 when !resetContainer:
                        break;
                    case 0:
                        containerName =
                            await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports, environment);
                        break;
                }

                await _runContainerCommand.ExecuteAsync(containerName);
            });
    }
}