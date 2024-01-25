using System.Diagnostics;
using port.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Config;

internal class ConfigCliCommand : Command<ConfigSettings>
{
    public override int Execute(CommandContext context, ConfigSettings settings)
    {
        ConfigFactory.GetOrCreateConfig();
        var path = ConfigFactory.GetConfigFilePath();

        if (!settings.Open)
        {
            AnsiConsole.WriteLine(FormatAsLink(path, path));
            return 0;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        return 0;
    }

    private static string FormatAsLink(string caption, string url) => $"\u001B]8;;{url}\a{caption}\u001B]8;;\a";
}