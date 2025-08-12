using Spectre.Console;
using Spectre.Console.Rendering;

namespace port.Spectre;

internal class CustomConsole : IAnsiConsole
{
    private readonly IAnsiConsole _console;

    public CustomConsole()
    {
        _console = AnsiConsole.Create(new AnsiConsoleSettings());
        Input = new CustomConsoleInput(_console.Input);
    }

    public void Clear(bool home) => _console.Clear(home);

    public void Write(IRenderable renderable) => _console.Write(renderable);

    public Profile Profile => _console.Profile;
    public IAnsiConsoleCursor Cursor => _console.Cursor;
    public IAnsiConsoleInput Input { get; }

    public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;
    public RenderPipeline Pipeline => _console.Pipeline;
}
