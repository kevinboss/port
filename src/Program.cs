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

// Query services
registrations.AddTransient<IAllImagesQuery, AllImagesQuery>();
registrations.AddTransient<IGetDigestsByIdQuery, GetDigestsByIdQuery>();
registrations.AddTransient<IGetImageQuery, GetImageQuery>();
registrations.AddTransient<IGetImageIdQuery, GetImageIdQuery>();
registrations.AddTransient<IDoesImageExistQuery, DoesImageExistQuery>();
registrations.AddTransient<IGetContainersQuery, GetContainersQuery>();
registrations.AddTransient<IGetRunningContainersQuery, GetRunningContainersQuery>();

// Command services
registrations.AddTransient<ICreateImageCommand, CreateImageCommand>();
registrations.AddTransient<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>();
registrations.AddTransient<ICreateContainerCommand, CreateContainerCommand>();
registrations.AddTransient<IRunContainerCommand, RunContainerCommand>();
registrations.AddTransient<IStopContainerCommand, StopContainerCommand>();
registrations.AddTransient<IStopAndRemoveContainerCommand, StopAndRemoveContainerCommand>();
registrations.AddTransient<IRemoveImageCommand, RemoveImageCommand>();
registrations.AddTransient<ICreateImageCliChildCommand, CreateImageCliChildCommand>();
registrations.AddTransient<IExportImageCommand, ExportImageCommand>();
registrations.AddTransient<IOrphanImageCommand, OrphanImageCommand>();
registrations.AddTransient<IImportImageCommand, ImportImageCommand>();
registrations.AddTransient<IRemoveImagesCliDependentCommand, RemoveImagesCliDependentCommand>();

// UI services
registrations.AddTransient<IImageIdentifierPrompt, ImageIdentifierPrompt>();
registrations.AddTransient<IContainerNamePrompt, ContainerNamePrompt>();
registrations.AddTransient<IProgressSubscriber, ProgressSubscriber>();

// Core services
registrations.AddTransient<IImageIdentifierAndTagEvaluator, ImageIdentifierAndTagEvaluator>();
registrations.AddSingleton<Config>(_ => ConfigFactory.GetOrCreateConfig());
registrations.AddSingleton<IDockerClient>(provider =>
{
    var config = provider.GetService<Config>() 
        ?? throw new InvalidOperationException("Failed to get Config service");
        
    if (config.DockerEndpoint is null)
        throw new InvalidOperationException("Docker endpoint has not been configured");
        
    return new DockerClientConfiguration(new Uri(config.DockerEndpoint)).CreateClient();
});

var registrar = new TypeRegistrar(registrations);

var app = new CommandApp(registrar);

app.Configure(appConfig =>
{
    appConfig.UseAssemblyInformationalVersion();
    appConfig.AddCommand<PullCliCommand>("pull")
        .WithAlias("p");
    appConfig.AddCommand<RunCliCommand>("run")
        .WithAlias("r");
    appConfig.AddCommand<ResetCliCommand>("reset")
        .WithAlias("rs");
    appConfig.AddCommand<CommitCliCommand>("commit")
        .WithAlias("c");
    appConfig.AddCommand<ListCliCommand>("list")
        .WithAlias("ls");
    appConfig.AddCommand<RemoveCliCommand>("remove")
        .WithAlias("rm");
    appConfig.AddCommand<PruneCliCommand>("prune")
        .WithAlias("pr");
    appConfig.AddCommand<StopCliCommand>("stop")
        .WithAlias("s");
    appConfig.AddCommand<ConfigCliCommand>("config")
        .WithAlias("cfg");
});

AnsiConsole.Console = new CustomConsole();

app.Configure(config =>
{
    config.SetExceptionHandler((exception, _) => exception switch
    {
        TimeoutException => HandleTimeoutException(),
        _ => HandleGenericException(exception)
    });

static int HandleTimeoutException()
{
    AnsiConsole.MarkupLine("[red]Timeout exception occurred[/], is the Docker daemon running?");
    return -1;
}

static int HandleGenericException(Exception exception)
{
    AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
    return -1;
}
});

return app.Run(args);
