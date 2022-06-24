using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneSettings : CommandSettings, IImageIdentifierSettings
{
    public string? ImageIdentifier { get; set; }
}