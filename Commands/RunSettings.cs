using Spectre.Console.Cli;

namespace dcma.Commands;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[ImageAlias]")]
    public string? ImageAlias { get; set; }
}