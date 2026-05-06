using Spectre.Console.Cli;

namespace port.Commands.Prune;

public class PruneSettings : CommandSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}
