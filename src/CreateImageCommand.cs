using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class CreateImageCommand : ICreateImageCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string imageName, string? tag)
    {
        var progress = new Progress<JSONMessage>();

        using (SubscribeToProgressChanged(progress))
        {
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = imageName,
                    Tag = tag
                },
                null, progress);
        }
    }

    private IDisposable SubscribeToProgressChanged(Progress<JSONMessage> progress)
    {
        var lockObject = new object();
        var publishedProgress = new Dictionary<string, Progress>();
        return Observable.FromEventPattern<JSONMessage>(
                h => progress.ProgressChanged += h,
                h => progress.ProgressChanged -= h)
            .Subscribe(pattern =>
            {
                lock (lockObject)
                {
                    HandleProgressMessage(pattern.EventArgs, publishedProgress);
                }
            });
    }

    private Subject<Progress> _progressSubjekt = new();

    public IObservable<Progress> ProgressObservable => _progressSubjekt.AsObservable();

    private void HandleProgressMessage(JSONMessage message, IDictionary<string, Progress> publishedProgress)
    {
        if (string.IsNullOrEmpty(message.ID))
            return;

        if (!publishedProgress.TryGetValue(message.ID, out var currentProgress))
        {
            var data = CreateTaskSetUpData(message);
            PublishInitialProgress(message, publishedProgress, data);
        }
        else
        {
            PublishUpdatedProgress(message, currentProgress);
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

    private void PublishInitialProgress(JSONMessage message,
        IDictionary<string, Progress> launchedTasks,
        TaskSetUpData data)
    {
        var progress = new Progress(ProgressState.Initial, message.ID, data);
        launchedTasks.Add(message.ID, progress);
        _progressSubjekt.OnNext(progress);
    }

    private void PublishUpdatedProgress(JSONMessage message, Progress currentProgress)
    {
        var progress = new Progress(currentProgress)
        {
            ProgressState = ProgressState.Downloading,
            Description = message.Status,
            ProgressMessage = message.ProgressMessage,
            CurrentProgress = message.Progress?.Current,
            TotalProgress = message.Progress?.Total
        };
        _progressSubjekt.OnNext(progress);
    }

    internal class TaskSetUpData
    {
        public string? Description { get; set; }
        public string? ProgressMessage { get; set; }
        public long? CurrentProgress { get; set; }
        public long? TotalProgress { get; set; }
    }
}