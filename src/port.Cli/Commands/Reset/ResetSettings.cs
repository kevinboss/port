using Spectre.Console.Cli;

namespace port.Commands.Reset;

public class ResetSettings : CommandSettings, IContainerIdentifierSettings
{
    [CommandArgument(0, "[ContainerIdentifier]")]
    public string? ContainerIdentifier { get; set; }
}
