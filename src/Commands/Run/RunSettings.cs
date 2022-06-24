using Spectre.Console.Cli;

namespace port.Commands.Run;

public class RunSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
    
    [CommandOption("-r|--reset")]
    public bool Reset { get; set; }
}