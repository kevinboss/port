using Spectre.Console.Cli;

namespace dcma.Commit;

internal class CommitCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;

    public CommitCommand(ICreateImageFromContainerCommand createImageFromContainerCommand, IGetRunningContainersQuery getRunningContainersQuery)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        settings.Tag ??= $"{DateTime.Now:yyyyMMddhhmmss}";

        var containerToCommit = await _getRunningContainersQuery.QueryAsync();

        if (containerToCommit == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await _createImageFromContainerCommand.ExecuteAsync(containerToCommit, settings.Tag);

        return 0;
    }
}