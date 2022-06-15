using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;
    private readonly Config.Config _config;

    public CommitCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainerQuery getRunningContainerQuery, Config.Config config)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainerQuery = getRunningContainerQuery;
        _config = config;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await _getRunningContainerQuery.QueryAsync();
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        var imageConfig = _config.GetImageByImageName(container.ImageName);

        var baseTag = imageConfig.ImageTags.SingleOrDefault(imageTag =>
                          container.ImageTag != null && container.ImageTag.StartsWith(imageTag))
                      ?? container.ImageTag;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Creating image from running container",
                _ => _createImageFromContainerCommand.ExecuteAsync(container.Id, container.ImageName, baseTag, tag));
        AnsiConsole.WriteLine($"Created image with tag {tag}");

        return 0;
    }
}