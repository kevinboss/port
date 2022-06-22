using Spectre.Console.Cli;

namespace port.Commands.Commit;

public class CommitSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")]
    public string? ImageIdentifier { get; set; }

    [CommandOption("-t|--tag")] 
    public string? Tag { get; set; }
}