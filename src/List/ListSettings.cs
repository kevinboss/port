using Spectre.Console.Cli;

namespace dcma.List;

public class ListSettings : CommandSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}