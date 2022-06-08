using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneSettings : CommandSettings, IIdentifierSettings
{
    public string? ImageIdentifier { get; set; }
}