using Spectre.Console.Cli;

namespace dcma.Commands;

public class ListSettings : CommandSettings
{
    [CommandArgument(0, "[ImageAlias]")] 
    public string? ImageAlias { get; set; }
}