using port.Commands.Remove;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Run;

internal class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly ICreateImageCliCommand _createImageCliCommand;
    private readonly IDoesImageExistQuery _doesImageExistQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly ITerminateContainersCommand _terminateContainersCommand;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;

    public RunCommand(IAllImagesQuery allImagesQuery, IIdentifierPrompt identifierPrompt,
        ICreateImageCliCommand createImageCliCommand, IDoesImageExistQuery doesImageExistQuery,
        IGetContainersQuery getContainersQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        ITerminateContainersCommand terminateContainersCommand, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand)
    {
        _allImagesQuery = allImagesQuery;
        _identifierPrompt = identifierPrompt;
        _createImageCliCommand = createImageCliCommand;
        _doesImageExistQuery = doesImageExistQuery;
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
        if (tag == null)
            throw new InvalidOperationException("Can not launch untagged image");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images",
                _ => TerminateOtherContainers(identifier, tag));
        await LaunchImageAsync(identifier, tag, settings.Reset);
        AnsiConsole.WriteLine($"Launched {ImageNameHelper.JoinImageNameAndTag(identifier, tag)}");
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetRunnableIdentifierFromUserAsync("run");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task TerminateOtherContainers(string identifier, string? tag)
    {
        var imageNames = await GetImageNamesExceptAsync(identifier, tag).ToListAsync();
        await _terminateContainersCommand.ExecuteAsync(imageNames);
    }

    private async IAsyncEnumerable<(string Name, string? Tag)> GetImageNamesExceptAsync(string identifier, string? tag)
    {
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            foreach (var image in imageGroup.Images.Where(image =>
                         imageGroup.Identifier != identifier || image.Tag != tag))
            {
                yield return (image.Name, image.Tag);
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
            .StartAsync($"Launching {ImageNameHelper.JoinImageNameAndTag(identifier, tag)}", async _ =>
            {
                await RemoveUntaggedContainersAndImageAsync(identifier, tag);
                var containers = (await _getContainersQuery.QueryByImageNameAndTagAsync(imageName, tag)).ToList();
                switch (containers.Count)
                {
                    case > 1:
                        throw new InvalidOperationException(
                            $"There should only be one container for {ImageNameHelper.JoinImageNameAndTag(identifier, tag)}");
                    case 1 when resetContainer:
                        await _stopAndRemoveContainerCommand.ExecuteAsync(containers.Single().Id);
                        await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports);
                        break;
                    case 0:
                        await _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports);
                        break;
                    case 1 when !resetContainer:
                        break;
                }

                await _runContainerCommand.ExecuteAsync(identifier, tag);
            });
    }

    private async Task RemoveUntaggedContainersAndImageAsync(string identifier, string tag)
    {
        var containers = (await _getContainersQuery.QueryByContainerNameAndTagAsync(identifier, tag)).ToList();
        if (!containers.Any())
        {
            return;
        }

        foreach (var container in containers.Where(e => e.ImageTag == null))
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            await _removeImageCommand.ExecuteAsync(
                ImageNameHelper.JoinImageNameAndTag(container.ImageName, container.ImageTag));
        }
    }
}