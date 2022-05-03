using dcma.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Remove;

public class RemoveCommand : AsyncCommand<RemoveSettings>
{
    private readonly IPromptHelper _promptHelper;
    private readonly IGetContainerQuery _getContainerQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly IConfig _config;

    public RemoveCommand(IPromptHelper promptHelper, IGetContainerQuery getContainerQuery, IConfig config,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand)
    {
        _promptHelper = promptHelper;
        _getContainerQuery = getContainerQuery;
        _config = config;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
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

    private async Task<(string identifier, string tag)> GetIdentifierAndTagAsync(RemoveSettings settings)
    {
        string? identifier;
        string? tag;
        if (settings.ImageIdentifier != null)
        {
            var identifierAndTag = DockerHelper.GetImageNameAndTag(settings.ImageIdentifier);
            identifier = identifierAndTag.imageName;
            tag = identifierAndTag.tag;
        }
        else
        {
            var identifierAndTag = await _promptHelper.GetIdentifierFromUserAsync("remove");
            identifier = identifierAndTag.identifier;
            tag = identifierAndTag.tag;
        }

        return (identifier, tag);
    }

    private async Task RemoveImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var containerListResponse = await _getContainerQuery.QueryAsync(imageName, tag);
        if (containerListResponse != null)
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(containerListResponse.ID);
        }

        await _removeImageCommand.ExecuteAsync(imageName, tag);
    }
}