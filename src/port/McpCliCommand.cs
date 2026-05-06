using port.Mcp;
using port.Spectre;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port;

public class McpCliCommand : AsyncCommand<McpSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, McpSettings settings)
    {
        AnsiConsole.Console = SilentConsole.Create();
        await McpHost.RunAsync();
        return 0;
    }
}
