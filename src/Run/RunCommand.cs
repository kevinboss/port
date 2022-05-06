using dcma.Config;
using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Run;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IPromptHelper _promptHelper;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetContainerQuery _getContainerQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly ITerminateContainersCommand _terminateContainersCommand;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;

    public RunCommand(IAllImagesQuery allImagesQuery, IPromptHelper promptHelper,
        ICreateImageCommand createImageCommand, IGetImageQuery getImageQuery, IGetContainerQuery getContainerQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        ITerminateContainersCommand terminateContainersCommand, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator)
    {
        _allImagesQuery = allImagesQuery;
        _promptHelper = promptHelper;
        _createImageCommand = createImageCommand;
        _getImageQuery = getImageQuery;
        _getContainerQuery = getContainerQuery;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _terminateContainersCommand = terminateContainersCommand;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images", _ => TerminateOtherContainers(identifier, tag));
        await LaunchImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string tag)> GetIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _promptHelper.GetIdentifierFromUserAsync("run");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task TerminateOtherContainers(string identifier, string? tag)
    {
        var imageNames = new List<(string imageName, string tag)>();
        await foreach (var imageName in GetImageNamesExceptAsync(identifier, tag))
        {
            imageNames.Add(imageName);
        }

        await _terminateContainersCommand.ExecuteAsync(imageNames);
    }

    private async IAsyncEnumerable<(string Name, string Tag)> GetImageNamesExceptAsync(string identifier, string? tag)
    {
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            foreach (var image in imageGroup.Images.Where(image => image.Identifier != identifier || image.Tag != tag))
            {
                yield return (image.Name, image.Tag);
            }
        }
    }

    private async Task LaunchImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        var ports = imageConfig.Ports;
        var containerListResponse = await _getContainerQuery.QueryAsync(imageName, tag);
        if (containerListResponse == null)
        {
            var imagesListResponse = await _getImageQuery.QueryAsync(imageName, tag);
            if (imagesListResponse == null)
            {
                await CreateImageAsync(identifier, tag, imageName);
            }

            await CreateContainerAsync(identifier, tag, imageName, ports);
        }

        await RunContainerAsync(identifier, tag);
    }

    private Task CreateImageAsync(string identifier, string tag, string imageName)
    {
        return AnsiConsole.Progress()
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
                await _createImageCommand.ExecuteAsync(imageName, tag, progress);
                progress.ProgressChanged -= OnProgressChanged;
            });
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

    private Task CreateContainerAsync(string identifier, string tag, string imageName, List<string> ports)
    {
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating container for {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports));
    }

    private Task RunContainerAsync(string identifier, string tag)
    {
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching container for {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _runContainerCommand.ExecuteAsync(identifier, tag));
    }

    private class TaskSetUpData
    {
        public string? Description { get; set; }
        public double? MaxValue { get; set; }
    }
}