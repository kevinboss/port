using Spectre.Console.Cli;

namespace dcma.Commands;

public class ListSettings : CommandSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}