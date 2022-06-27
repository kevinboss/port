using Spectre.Console;

namespace port;

internal class ContainerNamePrompt : IContainerNamePrompt
{
    public string GetIdentifierOfContainerFromUser(IReadOnlyCollection<Container> containers,
        string command)
    {
        switch (containers.Count)
        {
            case <= 0:
                throw new ArgumentException("Must contain at least 1 item", nameof(containers));
            case 1:
                return containers.Single().Name;
        }

        var selectionPrompt = CreateSelectionPrompt(command);
        foreach (var container in containers.OrderBy(i => i.Name))
        {
            selectionPrompt.AddChoice(container);
        }

        var selectedContainer = (Container)AnsiConsole.Prompt(selectionPrompt);
        return selectedContainer.Name;
    }

    private static SelectionPrompt<object> CreateSelectionPrompt(string command)
    {
        return new SelectionPrompt<object>()
            .UseConverter(o =>
            {
                return o switch
                {
                    Container container =>
                        $"[white]{container.Name}[/]",
                    _ => o as string ?? throw new InvalidOperationException()
                };
            })
            .PageSize(10)
            .Title($"Select container you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more containers)[/]");
    }
}