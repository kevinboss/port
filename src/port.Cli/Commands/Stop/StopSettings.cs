using Spectre.Console.Cli;

namespace port.Commands.Stop;

public class StopSettings : CommandSettings, IContainerIdentifierSettings
{
    [CommandArgument(0, "[ContainerIdentifier]")]
    public string? ContainerIdentifier { get; set; }
}
