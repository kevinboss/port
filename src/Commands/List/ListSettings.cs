using Spectre.Console.Cli;

namespace port.Commands.List;

public class ListSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}