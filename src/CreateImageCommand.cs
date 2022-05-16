using dcma.Run;
using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console;

namespace dcma;

internal class CreateImageCommand : ICreateImageCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string imageName, string tag)
    {
        AnsiConsole.WriteLine($"Downloading image {DockerHelper.JoinImageNameAndTag(imageName, tag)}");
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var progress = new Progress<JSONMessage>();
                var lockObject = new object();

                var taskSetUpData = new Dictionary<string, TaskSetUpData>();
                var tasks = new Dictionary<string, ProgressTask>();

                void OnProgressChanged(object? _, JSONMessage message)
                {
                    lock (lockObject)
                    {
                        HandleNewMessage(message, tasks, taskSetUpData, ctx);
                    }
                }

                progress.ProgressChanged += OnProgressChanged;
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = imageName,
                        Tag = tag
                    },
                    null,
                    progress);
                progress.ProgressChanged -= OnProgressChanged;
            });
        AnsiConsole.WriteLine($"Image {DockerHelper.JoinImageNameAndTag(imageName, tag)} downloaded");
    }

    private static void HandleNewMessage(JSONMessage message,
        IDictionary<string, ProgressTask> tasks,
        IDictionary<string, TaskSetUpData> taskSetUpData,
        ProgressContext ctx)
    {
        if (string.IsNullOrEmpty(message.ID))
            return;
        if (tasks.TryGetValue(message.ID, out var task))
        {
            if (!string.IsNullOrEmpty(message.Status))
                task.Description = message.Status;
            if (message.Progress is { Current: > 0 })
                task.Increment(message.Progress.Current - task.Value);
            return;
        }

        if (!taskSetUpData.TryGetValue(message.ID, out var data))
        {
            data = new TaskSetUpData();
            taskSetUpData.Add(message.ID, data);
        }

        if (data.Description == null && !string.IsNullOrEmpty(message.Status))
            data.Description = message.Status;
        if (data.MaxValue == null && message.Progress is { Total: > 0 })
            data.MaxValue = message.Progress.Total;
        if (data.Description != null && data.MaxValue.HasValue)
        {
            tasks.Add(message.ID, ctx.AddTask(data.Description, true, data.MaxValue.Value));
            taskSetUpData.Remove(message.ID);
        }
    }

    private class TaskSetUpData
    {
        public string? Description { get; set; }
        public double? MaxValue { get; set; }
    }
}