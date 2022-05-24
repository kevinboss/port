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

    public async Task ExecuteAsync(string imageName, string tag)
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
        var taskSetUpData = new Dictionary<string, TaskSetUpData>();
        var publishedProgress = new Dictionary<string, Progress>();
        return Observable.FromEventPattern<JSONMessage>(
                h => progress.ProgressChanged += h,
                h => progress.ProgressChanged -= h)
            .Subscribe(pattern =>
            {
                lock (lockObject)
                {
                    HandleProgressMessage(pattern.EventArgs, publishedProgress, taskSetUpData);
                }
            });
    }

    private Subject<Progress> _progressSubjekt = new();

    public IObservable<Progress> ProgressObservable => _progressSubjekt.AsObservable();

    private void HandleProgressMessage(JSONMessage message,
        IDictionary<string, Progress> publishedProgress,
        IDictionary<string, TaskSetUpData> taskSetUpData)
    {
        if (string.IsNullOrEmpty(message.ID))
            return;

        if (!publishedProgress.TryGetValue(message.ID, out var currentProgress))
        {
            var data = UpdateOrCreateTaskSetUpData(message, taskSetUpData);
            PublishInitialProgress(message, publishedProgress, taskSetUpData, data);
        }
        else
        {
            PublishUpdatedProgress(message, currentProgress);
        }
    }

    private void PublishInitialProgress(JSONMessage message,
        IDictionary<string, Progress> launchedTasks,
        IDictionary<string, TaskSetUpData> taskSetUpData,
        TaskSetUpData data)
    {
        var progress = new Progress(ProgressState.Initial, message.ID, data.Description, data.ProgressMessage);
        launchedTasks.Add(message.ID, progress);
        taskSetUpData.Remove(message.ID);
        _progressSubjekt.OnNext(progress);
    }

    private static TaskSetUpData UpdateOrCreateTaskSetUpData(JSONMessage message,
        IDictionary<string, TaskSetUpData> taskSetUpData)
    {
        if (!taskSetUpData.TryGetValue(message.ID, out var data))
        {
            data = new TaskSetUpData();
            taskSetUpData.Add(message.ID, data);
        }

        data.Description = message.Status;
        data.ProgressMessage = message.ProgressMessage;
        return data;
    }

    private void PublishUpdatedProgress(JSONMessage message, Progress currentProgress)
    {
        var progress = new Progress(ProgressState.Downloading, 
            currentProgress.Id, 
            currentProgress.Description,
            currentProgress.ProgressMessage)
        {
            Description = message.Status,
            ProgressMessage = message.ProgressMessage
        };
        _progressSubjekt.OnNext(progress);
    }

    private class TaskSetUpData
    {
        public string? Description { get; set; }
        public string? ProgressMessage { get; set; }
    }
}