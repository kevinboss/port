using Spectre.Console.Cli;

namespace dcma.Commands;

public class ListCommand : AsyncCommand<ListSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        throw new NotImplementedException();
    }
}