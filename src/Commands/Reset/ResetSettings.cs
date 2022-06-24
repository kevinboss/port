using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetSettings : CommandSettings, IContainerIdentifierSettings
{
    [CommandArgument(0, "[ContainerIdentifier]")]
    public string? ContainerIdentifier { get; set; }
}