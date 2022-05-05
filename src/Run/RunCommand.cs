using dcma.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Run;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IPromptHelper _promptHelper;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetContainerQuery _getContainerQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly ITerminateContainersCommand _terminateContainersCommand;
    private readonly IConfig _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;

    public RunCommand(IAllImagesQuery allImagesQuery, IPromptHelper promptHelper,
        ICreateImageCommand createImageCommand, IGetImageQuery getImageQuery, IGetContainerQuery getContainerQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        ITerminateContainersCommand terminateContainersCommand, IConfig config, IIdentifierAndTagEvaluator identifierAndTagEvaluator)
    {
        _allImagesQuery = allImagesQuery;
        _promptHelper = promptHelper;
        _createImageCommand = createImageCommand;
        _getImageQuery = getImageQuery;
        _getContainerQuery = getContainerQuery;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _terminateContainersCommand = terminateContainersCommand;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images", _ => TerminateOtherContainers(identifier, tag));
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => LaunchImageAsync(identifier, tag));
        return 0;
    }

    private async Task<(string identifier, string tag)> GetIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _promptHelper.GetIdentifierFromUserAsync("run");
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

    private async Task LaunchImageAsync(string identifier, string? tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var ports = imageConfig.Ports;
        var containerListResponse = await _getContainerQuery.QueryAsync(imageName, tag);
        if (containerListResponse == null)
        {
            var imagesListResponse = await _getImageQuery.QueryAsync(imageName, tag);
            if (imagesListResponse == null)
            {
                await _createImageCommand.ExecuteAsync(imageName, tag);
            }

            await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports);
        }

        await _runContainerCommand.ExecuteAsync(identifier, tag);
    }
}