using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;
    private readonly Config.Config _config;
    private readonly IGetImageQuery _getImageQuery;

    public CommitCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainerQuery getRunningContainerQuery, Config.Config config, IGetImageQuery getImageQuery)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainerQuery = getRunningContainerQuery;
        _config = config;
        _getImageQuery = getImageQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await _getRunningContainerQuery.QueryAsync();
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        var image = await _getImageQuery.QueryAsync(container.ImageName, container.ImageTag);
        if (image == null)
        {
            throw new InvalidOperationException(
                $"Image of running container {ImageNameHelper.JoinImageNameAndTag(container.ImageName, container.ImageTag)} not found");
        }

        while (image.Parent != null)
        {
            image = image.Parent;
        }

        var baseTag = image.Tag;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Creating image from running container",
                _ => _createImageFromContainerCommand.ExecuteAsync(container.Id, container.ImageName, baseTag, tag));
        AnsiConsole.WriteLine($"Created image with tag {tag}");

        return 0;
    }
}