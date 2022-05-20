using Spectre.Console.Cli;

namespace port.Commands.Run;

public class RunSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}