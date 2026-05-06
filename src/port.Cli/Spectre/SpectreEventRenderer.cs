using port.Orchestrators;
using Spectre.Console;

namespace port;

public static class SpectreEventRenderer
{
    private const string StatusTaskKey = "__status__";

    public static Task<T> WithRenderingAsync<TOrchestrator, T>(
        this TOrchestrator orchestrator,
        Func<TOrchestrator, Task<T>> work
    )
        where TOrchestrator : IOrchestrator =>
        RunAsync(orchestrator.Events, () => work(orchestrator));

    public static Task WithRenderingAsync<TOrchestrator>(
        this TOrchestrator orchestrator,
        Func<TOrchestrator, Task> work
    )
        where TOrchestrator : IOrchestrator =>
        RunAsync(
            orchestrator.Events,
            async () =>
            {
                await work(orchestrator);
                return 0;
            }
        );

    private static async Task<T> RunAsync<T>(
        IObservable<OrchestrationEvent> events,
        Func<Task<T>> work
    )
    {
        var tasks = new Dictionary<string, ProgressTask>();
        var lockObject = new object();

        return await AnsiConsole
            .Progress()
            .Columns(
                new PercentageColumn(),
                new ProgressBarColumn(),
                new SpinnerColumn(),
                new TaskDescriptionColumn { Alignment = Justify.Left }
            )
            .StartAsync(async ctx =>
            {
                using var subscription = events.Subscribe(evt =>
                    Apply(evt, ctx, tasks, lockObject)
                );
                return await work();
            });
    }

    private static void Apply(
        OrchestrationEvent evt,
        ProgressContext ctx,
        IDictionary<string, ProgressTask> tasks,
        object lockObject
    )
    {
        lock (lockObject)
        {
            switch (evt)
            {
                case StatusEvent status:
                    UpdateStatus(ctx, tasks, status.Message);
                    break;
                case LayerProgressEvent layer:
                    UpdateLayer(ctx, tasks, layer);
                    break;
                case WarningEvent warning:
                    AnsiConsole.MarkupLine($"[orange3]{warning.Message.EscapeMarkup()}[/]");
                    break;
            }
        }
    }

    private static void UpdateStatus(
        ProgressContext ctx,
        IDictionary<string, ProgressTask> tasks,
        string message
    )
    {
        var description = message.EscapeMarkup();
        if (!tasks.TryGetValue(StatusTaskKey, out var task))
        {
            task = ctx.AddTask(description, autoStart: true, maxValue: 1);
            task.IsIndeterminate();
            tasks[StatusTaskKey] = task;
        }
        else
        {
            task.Description = description;
        }
    }

    private static void UpdateLayer(
        ProgressContext ctx,
        IDictionary<string, ProgressTask> tasks,
        LayerProgressEvent layer
    )
    {
        var description = layer.Description?.EscapeMarkup() ?? string.Empty;
        var current = layer.Current ?? 0;
        var total = layer.Total ?? 100;
        if (total == 0 && current == 0)
        {
            current = 1;
            total = 1;
        }

        if (!tasks.TryGetValue(layer.LayerId, out var task))
        {
            task = ctx.AddTask(description, autoStart: true, maxValue: total);
            task.Value = current;
            if (layer.Current == null && layer.Total == null)
                task.IsIndeterminate();
            tasks[layer.LayerId] = task;
        }
        else
        {
            task.Description = description;
            task.MaxValue = total;
            task.Value = layer.Completed ? total : current;
        }
    }
}
