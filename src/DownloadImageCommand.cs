using Spectre.Console;

namespace dcma;

internal class DownloadImageCommand : IDownloadImageCommand
{
    private readonly ICreateImageCommand _createImageCommand;

    public DownloadImageCommand(ICreateImageCommand createImageCommand)
    {
        _createImageCommand = createImageCommand;
    }

    public async Task ExecuteAsync(string imageName, string tag)
    {
        AnsiConsole.WriteLine($"Downloading image {ImageNameHelper.JoinImageNameAndTag(imageName, tag)}");
        AnsiConsole.WriteLine();
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

        AnsiConsole.WriteLine($"Image {ImageNameHelper.JoinImageNameAndTag(imageName, tag)} downloaded");
    }
}