using Spectre.Console;

namespace port;

internal class CreateImageCliChildCommand : ICreateImageCliChildCommand
{
    private readonly ICreateImageCommand _createImageCommand;

    public CreateImageCliChildCommand(ICreateImageCommand createImageCommand)
    {
        _createImageCommand = createImageCommand;
    }

    public async Task ExecuteAsync(string imageName, string? tag)
    {
        var tasks = new Dictionary<string, ProgressTask>();
        var lockObject = new object();
        await AnsiConsole
            .Progress()
            .Columns(
                new PercentageColumn(),
                new ProgressBarColumn(),
                new SpinnerColumn(),
                new TaskDescriptionColumn { Alignment = Justify.Left }
            )
            .StartAsync(async ctx =>
            {
                _createImageCommand.ProgressObservable.Subscribe(progress =>
                    UpdateProgressTasks(tag, lockObject, progress, tasks, ctx)
                );
                await _createImageCommand.ExecuteAsync(imageName, tag);
            });
    }

    private static void UpdateProgressTasks(
        string? tag,
        object lockObject,
        Progress progress,
        IDictionary<string, ProgressTask> tasks,
        ProgressContext ctx
    )
    {
        lock (lockObject)
        {
            var description = progress.Description?.EscapeMarkup() ?? string.Empty;
            var currentProgress = progress.CurrentProgress ?? 0;
            var totalProgress = progress.TotalProgress ?? 100;
            if (totalProgress == currentProgress && totalProgress == 0)
            {
                currentProgress = 1;
                totalProgress = 1;
            }

            var key = progress.Id == tag ? Progress.NullId : progress.Id;
            if (!tasks.TryGetValue(key, out var task))
            {
                task = ctx.AddTask(description, true, totalProgress);
                task.Value = currentProgress;
                if (progress.CurrentProgress == null && progress.TotalProgress == null)
                    task.IsIndeterminate();
                tasks.Add(key, task);
            }
            else
            {
                task.Description = description;
                task.MaxValue = totalProgress;
                task.Value = progress.Id == Progress.NullId ? totalProgress : currentProgress;
            }
        }
    }
}
