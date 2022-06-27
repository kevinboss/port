using Spectre.Console.Cli;

namespace port.Commands.Stop;

internal class StopSettings : CommandSettings, IContainerIdentifierSettings
{
    [CommandArgument(0, "[ContainerIdentifier]")]
    public string? ContainerIdentifier { get; set; }
}