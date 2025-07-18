# Command Implementation Guidelines

## CLI Command Structure

When implementing a new CLI command, follow these patterns:

```csharp
namespace port.Commands.NewFeature;

public class NewFeatureCliCommand(
    // Use constructor injection with dependencies
    IRequiredService requiredService,
    Config config)
    : AsyncCommand<NewFeatureSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NewFeatureSettings settings)
    {
        // Implementation follows these steps:
        // 1. Parse/validate arguments
        // 2. Perform the action using injected services
        // 3. Display results using Spectre.Console
        // 4. Return 0 for success, non-zero for errors
        
        return 0;
    }
}
```

## Settings Class Implementation

When implementing a settings class for a command:

```csharp
namespace port.Commands.NewFeature;

public class NewFeatureSettings : CommandSettings
{
    [CommandArgument(0, "[ArgumentName]")]
    public string? ArgumentName { get; set; }
    
    [CommandOption("-o|--option")]
    public string? OptionName { get; set; }
}
```

## Registering Commands

Add new commands to Program.cs following this pattern:

```csharp
appConfig.AddCommand<NewFeatureCliCommand>("new-feature")
    .WithAlias("nf");
```

## Command Organization

1. Create a new folder in `Commands/` for the command functionality
2. The folder should contain:
   - `NewFeatureCliCommand.cs`: The command implementation
   - `NewFeatureSettings.cs`: Settings class for the command
   - Additional interfaces and implementations specific to the command

## Command Implementation (Action)

For command classes that perform actions but aren't CLI commands:

```csharp
namespace port;

public interface INewCommand
{
    Task<Result> ExecuteAsync(Parameters parameters);
}

public class NewCommand(IDockerClient dockerClient) : INewCommand
{
    public async Task<Result> ExecuteAsync(Parameters parameters)
    {
        // Implementation using dockerClient
        return result;
    }
}
```

## Error Handling

Always handle Docker API errors gracefully:

```csharp
try
{
    // Docker API call
}
catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    AnsiConsole.MarkupLine($"[red]Error: {errorMessage}[/]");
    return -1;
}
catch (TimeoutException)
{
    AnsiConsole.MarkupLine("[red]Timeout exception occurred[/], is the Docker daemon running?");
    return -1;
}
```

## Service Registration

Register new command services in Program.cs:

```csharp
registrations.AddTransient<INewCommand, NewCommand>();
```
