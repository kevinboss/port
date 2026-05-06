using System.Diagnostics;
using port.Orchestrators;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Config;

public class ConfigCliCommand(IConfigOrchestrator configOrchestrator) : Command<ConfigSettings>
{
    public override int Execute(CommandContext context, ConfigSettings settings)
    {
        var result = configOrchestrator.Execute();

        if (!settings.Open)
        {
            AnsiConsole.WriteLine(FormatAsLink(result.Path, result.Path));
            return 0;
        }

        Process.Start(new ProcessStartInfo(result.Path) { UseShellExecute = true });
        return 0;
    }

    private static string FormatAsLink(string caption, string url) =>
        $"]8;;{url}\a{caption}]8;;\a";
}
