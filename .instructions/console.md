# Console Output Guidelines

## Spectre.Console Usage

Port uses Spectre.Console for rich terminal output. Follow these patterns for consistent user experience.

## Tables

Use tables for structured data display:

```csharp
var table = new Table();
table.Border = TableBorder.Rounded;

// Add columns
table.AddColumn("Name");
table.AddColumn("Tag");
table.AddColumn("Status");

// Add rows
foreach (var image in images)
{
    table.AddRow(
        image.Name,
        image.Tag ?? "n/a",
        image.Running ? "[green]Running[/]" : "[gray]Stopped[/]"
    );
}

AnsiConsole.Write(table);
```

## Styled Markup

Use markup for colored and styled text:

```csharp
// Success message
AnsiConsole.MarkupLine($"[green]Successfully pulled image {name}:{tag}[/]");

// Error message
AnsiConsole.MarkupLine($"[red]Error: {errorMessage}[/]");

// Warning message
AnsiConsole.MarkupLine($"[yellow]Warning: {warningMessage}[/]");

// Information
AnsiConsole.MarkupLine($"[blue]Info: {infoMessage}[/]");
```

## Progress Indicators

Use spinners and progress bars for long-running operations:

```csharp
// Spinner for indeterminate progress
await AnsiConsole.Status()
    .StartAsync("Pulling image...", async ctx => 
    {
        ctx.Spinner(Spinner.Known.Dots);
        await LongRunningOperation();
    });

// Progress bar for deterministic progress
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[green]Downloading[/]");
        
        for (var i = 0; i <= 100; i++)
        {
            task.Value = i;
            await Task.Delay(50);
        }
    });

// For Docker progress updates
var progressSubscriber = new ProgressSubscriber();
await dockerClient.SomeOperation(parameters, progressSubscriber);
```

## User Prompts

For user interaction:

```csharp
// Selection
var selection = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select an image:")
        .AddChoices(imageOptions));

// Confirmation
if (AnsiConsole.Confirm("Are you sure you want to proceed?"))
{
    // Proceed with operation
}

// Text input
var name = AnsiConsole.Ask<string>("Enter container name:");
```

## Custom Console

Use the CustomConsole implementation when appropriate:

```csharp
// The project has a CustomConsole implementation
AnsiConsole.Console = new CustomConsole();
```

## Error Messages

Format error messages consistently:
1. Use red color for errors
2. Clearly state the issue
3. Provide actionable advice when possible

```csharp
AnsiConsole.MarkupLine("[red]Timeout exception occurred[/], is the Docker daemon running?");
```
