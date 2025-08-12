using Spectre.Console;

namespace port.Spectre;

internal class CustomConsoleInput : IAnsiConsoleInput
{
    private readonly IAnsiConsoleInput _input;

    public CustomConsoleInput(IAnsiConsoleInput input)
    {
        _input = input;
    }

    public bool IsKeyAvailable() => _input.IsKeyAvailable();

    public ConsoleKeyInfo? ReadKey(bool intercept) =>
        RewriteConsoleKeyInfo(_input.ReadKey(intercept));

    public async Task<ConsoleKeyInfo?> ReadKeyAsync(
        bool intercept,
        CancellationToken cancellationToken
    ) => RewriteConsoleKeyInfo(await _input.ReadKeyAsync(intercept, cancellationToken));

    private static ConsoleKeyInfo? RewriteConsoleKeyInfo(ConsoleKeyInfo? keyInfo)
    {
        if (keyInfo?.Key is not (ConsoleKey.J or ConsoleKey.K))
            return keyInfo;
        var shift = keyInfo.Value.Modifiers == ConsoleModifiers.Shift;
        var alt = keyInfo.Value.Modifiers == ConsoleModifiers.Alt;
        var control = keyInfo.Value.Modifiers == ConsoleModifiers.Control;
        return keyInfo.Value.Key switch
        {
            ConsoleKey.J => new ConsoleKeyInfo(
                keyInfo.Value.KeyChar,
                ConsoleKey.DownArrow,
                shift,
                alt,
                control
            ),
            ConsoleKey.K => new ConsoleKeyInfo(
                keyInfo.Value.KeyChar,
                ConsoleKey.UpArrow,
                shift,
                alt,
                control
            ),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
