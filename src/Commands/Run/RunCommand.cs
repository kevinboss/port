using System.Reactive.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands.Run;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetContainerQuery _getContainerQuery;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly ITerminateContainersCommand _terminateContainersCommand;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;

    public RunCommand(IAllImagesQuery allImagesQuery, IIdentifierPrompt identifierPrompt,
        ICreateImageCommand createImageCommand, IGetImageQuery getImageQuery, IGetContainerQuery getContainerQuery,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        ITerminateContainersCommand terminateContainersCommand, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator)
    {
        _allImagesQuery = allImagesQuery;
        _identifierPrompt = identifierPrompt;
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

        var identifierAndTag = await _identifierPrompt.GetIdentifierFromUserAsync("run");
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
                await CreateImageAsync(imageName, tag);
            }

            await CreateContainerAsync(identifier, tag, imageName, ports);
        }

        await RunContainerAsync(identifier, tag);
    }

    private async Task CreateImageAsync(string imageName, string tag)
    {
        AnsiConsole.WriteLine($"Downloading image {DockerHelper.JoinImageNameAndTag(imageName, tag)}");
        var tasks = new Dictionary<string, string>();
        var lockObject = new object();
        var table = new Table();
        table.HideHeaders();
        table.Border = TableBorder.None;
        table.AddColumn("Downloads");
        await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                using (_createImageCommand.ProgressObservable
                           .Subscribe(progress =>
                           {
                               lock (lockObject)
                               {
                                   var value =
                                       progress.ProgressMessage == null
                                           ? progress.Description.EscapeMarkup()
                                           : $"{progress.Description} {progress.ProgressMessage}".EscapeMarkup();
                                   if (progress.Initial)
                                   {
                                       tasks.Add(progress.Id, value);
                                       table.AddRow(value);
                                   }
                                   else
                                   {
                                       tasks.Remove(progress.Id);
                                       tasks.Add(progress.Id, value);
                                   }

                                   var row = 0;
                                   foreach (var task in tasks)
                                   {
                                       table.UpdateCell(row, 0, task.Value);
                                       row++;
                                   }

                                   ctx.Refresh();
                               }
                           }))
                {
                    await _createImageCommand.ExecuteAsync(imageName, tag);
                }
            });

        AnsiConsole.WriteLine($"Image {DockerHelper.JoinImageNameAndTag(imageName, tag)} downloaded");
    }

    private async Task CreateContainerAsync(string identifier, string tag, string imageName, List<string> ports)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating container for {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _createContainerCommand.ExecuteAsync(identifier, imageName, tag, ports));
        AnsiConsole.WriteLine($"Container for {DockerHelper.JoinImageNameAndTag(identifier, tag)} created");
    }

    private async Task RunContainerAsync(string identifier, string tag)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching container for {DockerHelper.JoinImageNameAndTag(identifier, tag)}",
                _ => _runContainerCommand.ExecuteAsync(identifier, tag));
        AnsiConsole.WriteLine($"Container for {DockerHelper.JoinImageNameAndTag(identifier, tag)} launched");
    }
}