using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}