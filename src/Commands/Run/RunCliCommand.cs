using port.Commands.Remove;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Run;

internal class RunCliCommand : AsyncCommand<RunSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly ICreateImageCliCommand _createImageCliCommand;
    private readonly IDoesImageExistQuery _doesImageExistQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;

    private const char PortSeparator = ':';

    public RunCliCommand(IIdentifierPrompt identifierPrompt,
        ICreateImageCliCommand createImageCliCommand, IDoesImageExistQuery doesImageExistQuery,
        IGetContainersQuery getContainersQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        IStopContainerCommand stopContainerCommand, Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand)
    {
        _identifierPrompt = identifierPrompt;
        _createImageCliCommand = createImageCliCommand;
        _doesImageExistQuery = doesImageExistQuery;
        _getContainersQuery = getContainersQuery;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _stopContainerCommand = stopContainerCommand;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not launch untagged image");
        await TerminateOtherContainersAsync(identifier, tag);
        await LaunchImageAsync(identifier, tag, settings.Reset);
        AnsiConsole.WriteLine($"Launched {ImageNameHelper.BuildImageName(identifier, tag)}");
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetRunnableIdentifierFromUserAsync("run");
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

    private async IAsyncEnumerable<Container> GetRunningContainersUsingHostPortsAsync(
        IReadOnlyCollection<string> hostPorts)
    {
        foreach (var container in await _getContainersQuery.QueryRunningAsync())
        {
            if (container.Ports.Any(p => hostPorts.Contains(p.PublicPort.ToString())))
            {
                yield return container;
            }
        }
    }

    private async Task LaunchImageAsync(string identifier, string tag, bool resetContainer)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var ports = imageConfig.Ports;
        if (!await _doesImageExistQuery.QueryAsync(imageName, tag))
            await _createImageCliCommand.ExecuteAsync(imageName, tag);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {ImageNameHelper.BuildImageName(identifier, tag)}", async _ =>
            {
                await RemoveUntaggedContainersAndImageAsync(identifier);
                var containers = (await _getContainersQuery.QueryByContainerNameAsync(ContainerNameHelper.BuildContainerName(identifier, tag))).ToList();
                switch (containers.Count)
                {
                    case 1 when resetContainer:
                        await _stopAndRemoveContainerCommand.ExecuteAsync(containers.Single().Id);
                        await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports);
                        break;
                    case 1 when !resetContainer:
                        break;
                    case 0:
                        await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports);
                        break;
                }

                await _runContainerCommand.ExecuteAsync(identifier, tag);
            });
    }

    private async Task RemoveUntaggedContainersAndImageAsync(string identifier)
    {
        var containers = (await _getContainersQuery.QueryByContainerNameAsync(identifier)).ToList();
        if (!containers.Any())
        {
            return;
        }

        foreach (var container in containers.Where(e => e.ImageTag == null))
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            await _removeImageCommand.ExecuteAsync(
                ImageNameHelper.BuildImageName(container.ImageName, container.ImageTag));
        }
    }
}