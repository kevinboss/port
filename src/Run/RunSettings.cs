using Spectre.Console.Cli;

namespace dcma.Run;

public class RunSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }
}