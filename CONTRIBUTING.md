# Contributing to port

Pull requests are welcome. Fork the repository, work on a branch, commit your
changes, and open a pull request.

## Prerequisites

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
Install [Docker](https://docs.docker.com/get-docker/) if you want to run `port`
against a local Docker daemon.

## Build and test

CI runs the following commands on every push:

```bash
dotnet restore src/port/port.csproj
dotnet build src/port/port.csproj --configuration Release
dotnet test src/port/port.csproj --no-restore --verbosity normal
```

To run the CLI from source during development:

```bash
dotnet run --project src/port/port.csproj -- list
```

Release builds produce a single-file executable (`PublishSingleFile` is enabled
in `src/port/port.csproj` when `Configuration` is `Release`).

## Repository layout

`src/port.sln` contains four .NET 10 projects sharing build settings in
`src/Directory.Build.props` and central NuGet versions in
`src/Directory.Packages.props`.

`src/port/` holds the entry point (`Program.cs`), dependency injection wiring,
command registration, and `McpCliCommand.cs`.

`src/port.Core/` holds YAML configuration and Docker Compose loading alongside
the Docker API queries, commands, and orchestrators shared by the CLI and MCP
server.

`src/port.Cli/` holds the Spectre.Console command handlers and interactive
pickers.

`src/port.Mcp/` holds the Model Context Protocol stdio host (`McpHost.cs`) and
tool definitions (`PortMcpTools.cs`).
