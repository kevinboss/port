using Spectre.Console.Cli;

namespace port.Commands.Import;

internal class ImportSettings : CommandSettings
{
    [CommandArgument(0, "<Tag>")]
    public string Tag { get; set; } = null!;

    [CommandOption("-p|--path <PATH>")] 
    public string Path { get; set; } = null!;
}