using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Orphan;

internal class OrphanCliCommand : AsyncCommand<OrphanSettings>
{
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly Config.Config _config;
    private readonly IOrphanImageCommand _orphanImageCommand;
    private readonly IGetImageIdQuery _getImageIdQuery;

    public OrphanCliCommand(IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IImageIdentifierPrompt imageIdentifierPrompt, Config.Config config, IOrphanImageCommand orphanImageCommand,
        IGetImageIdQuery getImageIdQuery)
    {
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _orphanImageCommand = orphanImageCommand;
        _getImageIdQuery = getImageIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, OrphanSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not export orphan image");
        await OrphanAsync(identifier, tag);
        return 0;
    }

    private async Task OrphanAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        AnsiConsole.WriteLine($"Orphaning image {ImageNameHelper.BuildImageName(imageName, tag)}");
        var imageId = (await _getImageIdQuery.QueryAsync(imageName, tag)).SingleOrDefault();
        if (imageId == null)
            throw new InvalidOperationException(
                $"No images for '{ImageNameHelper.BuildImageName(imageName, tag)}' do exist".EscapeMarkup());
        var tasks = new Dictionary<string, ProgressTask>();
        var lockObject = new object();
        await AnsiConsole.Progress()
            .Columns(
                new PercentageColumn(),
                new ProgressBarColumn(),
                new SpinnerColumn(),
                new TaskDescriptionColumn
                {
                    Alignment = Justify.Left
                })
            .StartAsync(async ctx =>
            {
                using (_orphanImageCommand.ProgressObservable
                           .Subscribe(progress =>
                           {
                               lock (lockObject)
                               {
                                   if (progress.Id == tag) return;
                                   var description = progress.Description?.EscapeMarkup() ?? string.Empty;
                                   var currentProgress = progress.CurrentProgress ?? 0;
                                   var totalProgress = progress.TotalProgress ?? 100;
                                   if (totalProgress == currentProgress && totalProgress == 0)
                                   {
                                       currentProgress = 1;
                                       totalProgress = 1;
                                   }

                                   if (!tasks.TryGetValue(progress.Id, out var task))
                                   {
                                       task = ctx.AddTask(description, true, totalProgress);
                                       task.Value = currentProgress;
                                       tasks.Add(progress.Id, task);
                                   }
                                   else
                                   {
                                       task.Description = description;
                                       task.Value = currentProgress;
                                       task.MaxValue = totalProgress;
                                   }
                               }
                           }))
                {
                    await _orphanImageCommand.ExecuteAsync(imageId);
                }
            });
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _imageIdentifierPrompt.GetDownloadedIdentifierAndTagFromUserAsync("orphan");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}