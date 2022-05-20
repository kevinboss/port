using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}