using Spectre.Console.Cli;

namespace port.Commands.Remove;

public class RemoveSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }

    [CommandOption("-r|--recursive")]
    public bool Recursive { get; set; }
}
