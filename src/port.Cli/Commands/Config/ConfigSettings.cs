using Spectre.Console.Cli;

namespace port.Commands.Config;

public class ConfigSettings : CommandSettings
{
    [CommandOption("-o|--open")]
    public bool Open { get; set; } = false;
}
