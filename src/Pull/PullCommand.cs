using dcma.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Pull;

public class PullCommand : AsyncCommand<PullSettings>
{
    private readonly IPromptHelper _promptHelper;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;

    public PullCommand(IPromptHelper promptHelper, Config.Config config, IIdentifierAndTagEvaluator identifierAndTagEvaluator)
    {
        _promptHelper = promptHelper;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => PullImageAsync(identifier, tag));
        return 0;
    }

    private async Task<(string identifier, string tag)> GetBaseIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _promptHelper.GetBaseIdentifierFromUserAsync("pull");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task PullImageAsync(string identifier, string? tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
    }
}