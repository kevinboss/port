using Spectre.Console.Cli;

namespace dcma.Commands;

public class CommitSettings : CommandSettings
{
    [CommandOption("-t|--tag")]
    public string? Tag { get; set; }
}