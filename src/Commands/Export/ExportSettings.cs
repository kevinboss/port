using Spectre.Console.Cli;

namespace port.Commands.Export;

internal class ExportSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }

    [CommandOption("-p|--path <PATH>")] 
    public string Path { get; set; } = null!;
}