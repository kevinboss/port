using Spectre.Console;

namespace port.Spectre;

public static class SilentConsole
{
    public static IAnsiConsole Create() =>
        AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Interactive = InteractionSupport.No,
                Out = new AnsiConsoleOutput(TextWriter.Null),
            }
        );
}
