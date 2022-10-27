using Spectre.Console.Cli;

namespace port.Commands.Orphan;

internal class OrphanSettings : CommandSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}