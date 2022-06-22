using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCliCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IIdentifierPrompt _identifierPrompt;

    public CommitCliCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainersQuery getRunningContainersQuery, IGetImageQuery getImageQuery,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator, IIdentifierPrompt identifierPrompt)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
        _getImageQuery = getImageQuery;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
        _identifierPrompt = identifierPrompt;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await GetContainerAsync(settings);
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

    private async Task<Container?> GetContainerAsync(IIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync();
        if (containers.Count <= 0) return containers.SingleOrDefault();
        if (settings.ImageIdentifier != null)
        {
            var (identifier, tag) = _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
        else
        {
            var (identifier, tag) = _identifierPrompt.GetIdentifierOfContainerFromUser(containers, "reset");
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
    }
}