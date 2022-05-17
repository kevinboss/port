using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Remove;

public class RemoveCommand : AsyncCommand<RemoveSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IGetContainerQuery _getContainerQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;

    public RemoveCommand(IIdentifierPrompt identifierPrompt, IGetContainerQuery getContainerQuery, Config.Config config,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator)
    {
        _identifierPrompt = identifierPrompt;
        _getContainerQuery = getContainerQuery;
        _config = config;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => RemoveImageAsync(identifier, tag));
        return 0;
    }

    private async Task<(string identifier, string tag)> GetIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetIdentifierFromUserAsync("remove", true);
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task RemoveImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var containerListResponse = await _getContainerQuery.QueryAsync(imageName, tag);
        if (containerListResponse != null)
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(containerListResponse.ID);
        }

        await _removeImageCommand.ExecuteAsync(imageName, tag);
        AnsiConsole.WriteLine($"Removed image {DockerHelper.JoinImageNameAndTag(imageName, tag)}");
    }
}