using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet.Models;

namespace port;

internal class ProgressSubscriber : IProgressSubscriber
{
    public IDisposable Subscribe(Progress<JSONMessage> progress, IObserver<Progress> observer)
    {
        object lockObject = new();
        lock (lockObject)
        {
            var publishedProgress = new Dictionary<string, Progress>();
            return Observable.FromEventPattern<JSONMessage>(
                    h => progress.ProgressChanged += h,
                    h => progress.ProgressChanged -= h)
                .Subscribe(pattern =>
                {
                    lock (lockObject)
                    {
                        HandleProgressMessage(pattern.EventArgs, publishedProgress, observer);
                    }
                });
        }
    }

    private static void HandleProgressMessage(JSONMessage message, IDictionary<string, Progress> publishedProgress,
        IObserver<Progress> observer)
    {
        if (string.IsNullOrEmpty(message.ID))
            message.ID = Progress.NullId;

        if (!publishedProgress.TryGetValue(message.ID, out var currentProgress))
        {
            var data = CreateTaskSetUpData(message);
            PublishInitialProgress(message, publishedProgress, data, observer);
        }
        else
        {
            PublishUpdatedProgress(message, currentProgress, observer);
        }
    }

    private static TaskSetUpData CreateTaskSetUpData(JSONMessage message)
    {
        var data = new TaskSetUpData
        {
            Description = message.Status,
            ProgressMessage = message.ProgressMessage,
            CurrentProgress = message.Progress?.Current,
            TotalProgress = message.Progress?.Total
        };
        return data;
    }

    private static void PublishInitialProgress(JSONMessage message,
        IDictionary<string, Progress> launchedTasks,
        TaskSetUpData data, IObserver<Progress> progressSubjekt)
    {
        var progress = new Progress(ProgressState.Initial, message.ID, data);
        launchedTasks.Add(message.ID, progress);
        progressSubjekt.OnNext(progress);
    }

    private static void PublishUpdatedProgress(JSONMessage message,
        Progress currentProgress,
        IObserver<Progress> progressSubjekt)
    {
        var progress = new Progress(currentProgress)
        {
            ProgressState = ProgressState.Downloading,
            Description = message.Status,
            ProgressMessage = message.ProgressMessage,
            CurrentProgress = message.Progress?.Current,
            TotalProgress = message.Progress?.Total
        };
        progressSubjekt.OnNext(progress);
    }

    internal class TaskSetUpData
    {
        public string? Description { get; set; }
        public string? ProgressMessage { get; set; }
        public long? CurrentProgress { get; set; }
        public long? TotalProgress { get; set; }
    }
}