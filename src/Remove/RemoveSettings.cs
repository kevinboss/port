using Spectre.Console.Cli;

namespace dcma.Remove;

public class RemoveSettings : CommandSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}