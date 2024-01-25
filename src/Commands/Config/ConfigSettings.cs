using Spectre.Console.Cli;

namespace port.Commands.Config;

internal class ConfigSettings : CommandSettings
{
    [CommandOption("-o|--open")]
    public bool Open { get; set; } = false;
}