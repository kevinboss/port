using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetSettings : CommandSettings, IContainerIdentifierSettings, IImageIdentifierSettings
{
    [CommandArgument(0, "[Identifier]")]
    public string? ContainerIdentifier { get; set; }

    [CommandOption("-i|--image")]
    public bool IsImage { get; set; }

    [CommandOption("-t|--tag")]
    public string? Tag { get; set; }

    string? IImageIdentifierSettings.ImageIdentifier => ContainerIdentifier;
}
