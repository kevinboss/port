using port.Commands.List;
using port.Orchestrators;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Remove;

public class RemoveCliCommand : AsyncCommand<RemoveSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IRemoveOrchestrator _removeOrchestrator;
    private readonly ListCliCommand _listCliCommand;

    public RemoveCliCommand(
        IImageIdentifierPrompt imageIdentifierPrompt,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IRemoveOrchestrator removeOrchestrator,
        ListCliCommand listCliCommand
    )
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _removeOrchestrator = removeOrchestrator;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await ResolveIdentifierAndTagAsync(settings);
        var result = await _removeOrchestrator.WithRenderingAsync(o =>
            o.ExecuteAsync(identifier, tag, settings.Recursive)
        );

        foreach (var failure in result.Removals.Where(r => !r.Successful))
        {
            AnsiConsole.MarkupLine(
                $"[orange3]Unable to removed image with id '{failure.ImageId}'[/] because it has dependent children"
            );
            if (settings.Recursive)
                AnsiConsole.MarkupLine(
                    "That may be because an child image is based on an [red]unknown image[/] which can not be removed automatically, manually remove it and try again"
                );
        }

        await _listCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<(string identifier, string? tag)> ResolveIdentifierAndTagAsync(
        RemoveSettings settings
    )
    {
        if (settings.ImageIdentifier != null)
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);

        var identifierAndTag = await _imageIdentifierPrompt.GetDownloadedIdentifierAndTagFromUserAsync(
            "remove"
        );
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}
