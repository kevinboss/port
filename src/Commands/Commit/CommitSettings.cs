using Spectre.Console.Cli;

namespace port.Commands.Commit;

public class CommitSettings : CommandSettings, IContainerIdentifierSettings
{
    [CommandArgument(0, "[ContainerIdentifier]")]
    public string? ContainerIdentifier { get; set; }

    [CommandOption("-t|--tag")] 
    public string? Tag { get; set; }

    [CommandOption("-s|--switch")] 
    public bool Switch { get; set; }
}