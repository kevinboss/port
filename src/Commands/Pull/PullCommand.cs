using System.Reactive.Linq;
using Docker.DotNet;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands.Pull;

public class PullCommand : AsyncCommand<PullSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly Config.Config _config;
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IDockerClient _dockerClient;
    private readonly ICreateImageCommand _createImageCommand;

    public PullCommand(IIdentifierPrompt identifierPrompt, Config.Config config,
        IIdentifierAndTagEvaluator identifierAndTagEvaluator, IDockerClient dockerClient,
        ICreateImageCommand createImageCommand)
    {
        _identifierPrompt = identifierPrompt;
        _config = config;
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
        _dockerClient = dockerClient;
        _createImageCommand = createImageCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await PullImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string tag)> GetBaseIdentifierAndTagAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetBaseIdentifierFromUserAsync("pull");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task PullImageAsync(string identifier, string tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
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

    private IDisposable SubscribeToProgressObserable(object lockObject, IDictionary<string, string> tasks)
    {
        return _createImageCommand.ProgressObservable
            .Subscribe(progress =>
            {
                lock (lockObject)
                {
                    if (progress.Initial)
                        tasks.Add(progress.Id, $"{progress.Description} {progress.ProgressMessage}");
                    else
                    {
                        tasks.Remove(progress.Id);
                        tasks.Add(progress.Id, $"{progress.Description} {progress.ProgressMessage}");
                    }

                    foreach (var keyValuePair in tasks.OrderBy(e => e.Key))
                    {
                        AnsiConsole.Write($"{keyValuePair.Value}");
                    }
                }
            });
    }
}