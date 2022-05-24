using port.Commands.Remove;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Run;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IDownloadImageCommand _downloadImageCommand;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly ITerminateContainersCommand _terminateContainersCommand;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;

    public RunCommand(IAllImagesQuery allImagesQuery, IIdentifierPrompt identifierPrompt,
        IDownloadImageCommand downloadImageCommand, IGetImageQuery getImageQuery,
        IGetContainersQuery getContainersQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        ITerminateContainersCommand terminateContainersCommand, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand)
    {
        _allImagesQuery = allImagesQuery;
        _identifierPrompt = identifierPrompt;
        _downloadImageCommand = downloadImageCommand;
        _getImageQuery = getImageQuery;
        _getContainersQuery = getContainersQuery;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _terminateContainersCommand = terminateContainersCommand;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images", _ => TerminateOtherContainers(identifier, tag));
        await LaunchImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string tag)> GetIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetIdentifierFromUserAsync("run");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task TerminateOtherContainers(string identifier, string? tag)
    {
        var imageNames = new List<(string imageName, string tag)>();
        await foreach (var imageName in GetImageNamesExceptAsync(identifier, tag))
        {
            imageNames.Add(imageName);
        }

        await _terminateContainersCommand.ExecuteAsync(imageNames);
    }

    private async IAsyncEnumerable<(string Name, string Tag)> GetImageNamesExceptAsync(string identifier, string? tag)
    {
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            foreach (var image in imageGroup.Images.Where(image => image.Identifier != identifier || image.Tag != tag))
            {
                yield return (image.Name, image.Tag);
            }
        }
    }

    private async Task LaunchImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var ports = imageConfig.Ports;
        var container = await _getContainersQuery.QueryByImageAsync(imageName, tag);
        if (container == null)
        {
            var imagesListResponse = await _getImageQuery.QueryAsync(imageName, tag);
            if (imagesListResponse == null)
            {
                await _downloadImageCommand.ExecuteAsync(imageName, tag);
            }

            await RemoveContainerByIdentifierAsync(identifier, tag);

            await CreateContainerAsync(identifier, tag, imageName, ports);
        }

        await RunContainerAsync(identifier, tag);
    }

    private async Task RemoveContainerByIdentifierAsync(string identifier, string tag)
    {
        var containers = (await _getContainersQuery.QueryByIdentifierAsync(identifier, tag)).ToList();
        if (!containers.Any())
        {
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing containers for {ContainerNameHelper.JoinContainerNameAndTag(identifier, tag)}",
                async _ =>
                {
                    foreach (var container in containers)
                    {
                        await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                        await _removeImageCommand.ExecuteAsync(container.ImageName, container.Tag);
                    }
                });
        AnsiConsole.WriteLine($"Containers for {ContainerNameHelper.JoinContainerNameAndTag(identifier, tag)} removed");
    }

    private async Task CreateContainerAsync(string identifier, string tag, string imageName, List<string> ports)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating container for {ImageNameHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports));
        AnsiConsole.WriteLine($"Container for {ImageNameHelper.JoinImageNameAndTag(identifier, tag)} created");
    }

    private async Task RunContainerAsync(string identifier, string tag)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching container for {ImageNameHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _runContainerCommand.ExecuteAsync(identifier, tag));
        AnsiConsole.WriteLine($"Container for {ImageNameHelper.JoinImageNameAndTag(identifier, tag)} launched");
    }
}