using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;

    public CommitCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainerQuery getRunningContainerQuery)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainerQuery = getRunningContainerQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await _getRunningContainerQuery.QueryAsync();
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Creating image from running container",
                _ => _createImageFromContainerCommand.ExecuteAsync(container, tag));
        AnsiConsole.WriteLine($"Created image with tag {tag}");

        return 0;
    }
}