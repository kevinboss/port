using Spectre.Console;

namespace port;

internal class DownloadImageCommand : IDownloadImageCommand
{
    private readonly ICreateImageCommand _createImageCommand;

    public DownloadImageCommand(ICreateImageCommand createImageCommand)
    {
        _createImageCommand = createImageCommand;
    }

    public async Task ExecuteAsync(string imageName, string? tag)
    {
        AnsiConsole.WriteLine($"Downloading image {ImageNameHelper.JoinImageNameAndTag(imageName, tag)}");
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
                using (_createImageCommand.ProgressObservable
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
                    await _createImageCommand.ExecuteAsync(imageName, tag);
                }
            });

        AnsiConsole.WriteLine($"Finished downloading Image {ImageNameHelper.JoinImageNameAndTag(imageName, tag)}");
    }
}