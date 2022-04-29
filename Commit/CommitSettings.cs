using Spectre.Console.Cli;

namespace dcma.Commit;

public class CommitSettings : CommandSettings
{
    [CommandOption("-t|--tag")]
    public string? Tag { get; set; }
}