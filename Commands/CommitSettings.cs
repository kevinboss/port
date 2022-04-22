using Spectre.Console.Cli;

namespace dcma.Commands;

public class CommitSettings : CommandSettings
{
    [CommandOption("-i|--identifier")]
    public string? Identifier { get; set; }
}