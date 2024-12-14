using System.Reflection;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using port;
using port.Commands.Commit;
using port.Commands.Config;
using port.Commands.Export;
using port.Commands.Import;
using port.Commands.List;
using port.Commands.Orphan;
using port.Commands.Prune;
using port.Commands.Pull;
using port.Commands.Remove;
using port.Commands.Reset;
using port.Commands.Run;
using port.Commands.Stop;
using port.Config;
using port.Infrastructure;
using port.Spectre;
using Spectre.Console;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();

// Register queries
registrations.AddTransient<IAllImagesQuery, AllImagesQuery>()
    .AddTransient<IGetDigestsByIdQuery, GetDigestsByIdQuery>()
    .AddTransient<IGetImageQuery, GetImageQuery>()
    .AddTransient<IGetImageIdQuery, GetImageIdQuery>()
    .AddTransient<IDoesImageExistQuery, DoesImageExistQuery>()
    .AddTransient<IGetContainersQuery, GetContainersQuery>()
    .AddTransient<IGetRunningContainersQuery, GetRunningContainersQuery>();

// Register commands
registrations.AddTransient<ICreateImageCommand, CreateImageCommand>()
    .AddTransient<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>()
    .AddTransient<ICreateContainerCommand, CreateContainerCommand>()
    .AddTransient<IRunContainerCommand, RunContainerCommand>()
    .AddTransient<IStopContainerCommand, StopContainerCommand>()
    .AddTransient<IStopAndRemoveContainerCommand, StopAndRemoveContainerCommand>()
    .AddTransient<IRemoveImageCommand, RemoveImageCommand>()
    .AddTransient<ICreateImageCliChildCommand, CreateImageCliChildCommand>()
    .AddTransient<IExportImageCommand, ExportImageCommand>()
    .AddTransient<IOrphanImageCommand, OrphanImageCommand>()
    .AddTransient<IImportImageCommand, ImportImageCommand>()
    .AddTransient<IRemoveImagesCliDependentCommand, RemoveImagesCliDependentCommand>();

// Register UI and helpers
registrations.AddTransient<IImageIdentifierPrompt, ImageIdentifierPrompt>()
    .AddTransient<IContainerNamePrompt, ContainerNamePrompt>()
    .AddTransient<IImageIdentifierAndTagEvaluator, ImageIdentifierAndTagEvaluator>()
    .AddTransient<IProgressSubscriber, ProgressSubscriber>();

// Register singletons
registrations.AddSingleton(typeof(Config), _ => ConfigFactory.GetOrCreateConfig())
    .AddSingleton(typeof(IDockerClient), provider =>
    {
        var config = provider.GetService<Config>();
        if (config?.DockerEndpoint == null)
            throw new InvalidOperationException("Docker endpoint has not been configured");
        return new DockerClientConfiguration(new Uri(config.DockerEndpoint)).CreateClient();
    });

var app = new CommandApp(new TypeRegistrar(registrations));

app.Configure(config =>
{
    config.UseAssemblyInformationalVersion();

    // Register CLI commands with aliases
    config.AddCommand<PullCliCommand>("pull").WithAlias("p")
        .AddCommand<RunCliCommand>("run").WithAlias("r")
        .AddCommand<ResetCliCommand>("reset").WithAlias("rs")
        .AddCommand<CommitCliCommand>("commit").WithAlias("c")
        .AddCommand<ListCliCommand>("list").WithAlias("ls")
        .AddCommand<RemoveCliCommand>("remove").WithAlias("rm")
        .AddCommand<PruneCliCommand>("prune").WithAlias("pr")
        .AddCommand<StopCliCommand>("stop").WithAlias("s")
        .AddCommand<ConfigCliCommand>("config").WithAlias("cfg");

    // Configure exception handling
    config.SetExceptionHandler((exception, _) => exception switch
    {
        TimeoutException => AnsiConsole.MarkupLine("[red]Timeout exception occurred[/], is the Docker daemon running?") - 2,
        _ => AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything) - 2
    });
});

AnsiConsole.Console = new CustomConsole();

return app.Run(args);
