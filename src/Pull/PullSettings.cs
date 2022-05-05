using Spectre.Console.Cli;

namespace dcma.Pull;

public class PullSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}