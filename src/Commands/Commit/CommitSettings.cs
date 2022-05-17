using Spectre.Console.Cli;

namespace dcma.Commands.Commit;

public class CommitSettings : CommandSettings
{
    [CommandOption("-t|--tag")]
    public string? Tag { get; set; }
}