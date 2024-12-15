using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public required string? ImageIdentifier { get; set; }
}
