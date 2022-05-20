using Spectre.Console.Cli;

namespace port.Commands.Remove;

public class RemoveSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}