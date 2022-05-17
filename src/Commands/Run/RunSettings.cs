using Spectre.Console.Cli;

namespace dcma.Commands.Run;

public class RunSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}