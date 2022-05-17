using Docker.DotNet;
using Spectre.Console.Cli;

namespace dcma.Commands.Pull;

public class PullCommand : AsyncCommand<PullSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IDockerClient _dockerClient;
    private readonly ICreateImageCommand _createImageCommand;

    public PullCommand(IIdentifierPrompt identifierPrompt, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator, IDockerClient dockerClient,
        ICreateImageCommand createImageCommand)
    {
        _identifierPrompt = identifierPrompt;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
        _dockerClient = dockerClient;
        _createImageCommand = createImageCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await PullImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string tag)> GetBaseIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetBaseIdentifierFromUserAsync("pull");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task PullImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        await _createImageCommand.ExecuteAsync(imageName, tag);
    }
}