# Port Project Coding Guidelines

## Project Overview
Port is a .NET CLI application that provides a simplified interface for Docker container management using clean architecture principles with dependency injection, command pattern, and CQRS.

## Architecture
- **Clean Architecture**: Separates concerns with interfaces and implementations
- **CLI Commands**: Using Spectre.Console.Cli with command pattern
- **Docker Integration**: Uses Docker.DotNet for API interaction
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection

## Directory Structure
- **Commands/**: Organized by functionality (Commit, Config, List, Pull, etc.)
- **Config/**: Configuration classes with versioning and migrations
- **Infrastructure/**: DI infrastructure (TypeRegistrar, TypeResolver)
- **Spectre/**: Custom console implementations

## Coding Patterns

### Dependency Injection
- Use constructor injection for dependencies
- Register all services in Program.cs
- Prefer primary constructors for dependency injection:
```csharp
public class PullCliCommand(IImageIdentifierPrompt prompt, Config config) : AsyncCommand<PullSettings>
```

### Interface-Driven Design
- Define capabilities through interfaces (I-prefixed)
- Implement concrete classes for each interface
- Example: `IAllImagesQuery` -> `AllImagesQuery`

### Async Programming
- Use async/await pattern with Task and IAsyncEnumerable
- All async methods should end with 'Async'
- Example: `Task<List<Image>> QueryByImageConfigAsync(Config.ImageConfig config);`

### Command Line Interface
- Commands follow Spectre.Console.Cli patterns
- Each command has a Settings class for parameters
- Command classes are suffixed with `CliCommand`
- Settings classes inherit from `CommandSettings`

## Coding Style

### Nullability
- Use nullable reference types
- Properties that can't be null: `public string Name { get; set; } = null!;`
- Properties that can be null: `public string? Tag { get; set; }`

### Naming Conventions
- **Interfaces**: Prefixed with 'I' (e.g., `IImageQuery`)
- **Commands**: Suffixed with 'CliCommand' (e.g., `PullCliCommand`)
- **Settings**: Suffixed with 'Settings' (e.g., `PullSettings`)
- **Queries**: Suffixed with 'Query' (e.g., `AllImagesQuery`)
- **Command Implementations**: Suffixed with 'Command' (e.g., `CreateContainerCommand`)
- **Helper Classes**: Suffixed with 'Helper' (e.g., `ImageNameHelper`)

## CQRS Pattern
- **Queries**: For data retrieval, suffixed with 'Query'
  - Example: `IGetImageQuery` -> `GetImageQuery`
- **Commands**: For actions, suffixed with 'Command'
  - Example: `IRemoveImageCommand` -> `RemoveImageCommand`

## Domain Models
- **Image**: Represents Docker image with properties for identification and tags
- **Container**: Represents Docker container with state properties
- **ImageGroup**: Represents logical grouping of related images

## CLI Command Structure
Each command follows the pattern:
- `<Command>CliCommand` class implementing `AsyncCommand<TSettings>`
- `<Command>Settings` class implementing `CommandSettings`
- Supporting interfaces and implementations for the command's functionality

## When Adding New Features

### Adding New Commands
1. Create a new folder in `Commands/` for the command
2. Create a settings class: `[CommandName]Settings.cs`
3. Create a command class: `[CommandName]CliCommand.cs`
4. Register the command in Program.cs with an alias

### Adding New Queries/Commands
1. Define an interface: `I[Name]Query.cs` or `I[Name]Command.cs`
2. Implement the interface: `[Name]Query.cs` or `[Name]Command.cs`
3. Register in Program.cs using dependency injection

## Error Handling
- Use try-catch for recoverable errors
- Let fatal exceptions propagate to global handler in Program.cs
- Provide clear error messages with potential solutions

## Build Configuration
- Target Framework: net9.0
- Nullable: enable
- Single file publish for Release builds
