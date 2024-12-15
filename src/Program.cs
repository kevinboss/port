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
List<(Type service, Type implementation)> transientServices = 
[
    (typeof(IAllImagesQuery), typeof(AllImagesQuery)),
    (typeof(IGetDigestsByIdQuery), typeof(GetDigestsByIdQuery)),
    (typeof(IImageIdentifierPrompt), typeof(ImageIdentifierPrompt)),
    (typeof(IContainerNamePrompt), typeof(ContainerNamePrompt)),
    (typeof(ICreateImageCommand), typeof(CreateImageCommand)),
    (typeof(ICreateImageFromContainerCommand), typeof(CreateImageFromContainerCommand)),
    (typeof(IGetImageQuery), typeof(GetImageQuery)),
    (typeof(IGetImageIdQuery), typeof(GetImageIdQuery)),
    (typeof(IDoesImageExistQuery), typeof(DoesImageExistQuery)),
    (typeof(IGetContainersQuery), typeof(GetContainersQuery)),
    (typeof(IGetRunningContainersQuery), typeof(GetRunningContainersQuery)),
    (typeof(ICreateContainerCommand), typeof(CreateContainerCommand)),
    (typeof(IRunContainerCommand), typeof(RunContainerCommand)),
    (typeof(IStopContainerCommand), typeof(StopContainerCommand)),
    (typeof(IStopAndRemoveContainerCommand), typeof(StopAndRemoveContainerCommand)),
    (typeof(IRemoveImageCommand), typeof(RemoveImageCommand)),
    (typeof(ICreateImageCliChildCommand), typeof(CreateImageCliChildCommand)),
    (typeof(IImageIdentifierAndTagEvaluator), typeof(ImageIdentifierAndTagEvaluator)),
    (typeof(IExportImageCommand), typeof(ExportImageCommand)),
    (typeof(IProgressSubscriber), typeof(ProgressSubscriber)),
    (typeof(IOrphanImageCommand), typeof(OrphanImageCommand)),
    (typeof(IImportImageCommand), typeof(ImportImageCommand)),
    (typeof(IRemoveImagesCliDependentCommand), typeof(RemoveImagesCliDependentCommand))
];

foreach (var (service, implementation) in transientServices)
{
    registrations.AddTransient(service, implementation);
}
registrations.AddSingleton(typeof(Config), _ => ConfigFactory.GetOrCreateConfig());
registrations.AddSingleton(typeof(IDockerClient), provider =>
{
    var config = provider.GetService<Config>();
    if (config?.DockerEndpoint == null)
        throw new InvalidOperationException("Docker endpoint has not been configured");
    var endpoint = new Uri(config.DockerEndpoint);
    return new DockerClientConfiguration(endpoint).CreateClient();
});

var registrar = new TypeRegistrar(registrations);

var app = new CommandApp(registrar);

List<(Type commandType, string name, string alias)> commands = 
[
    (typeof(PullCliCommand), "pull", "p"),
    (typeof(RunCliCommand), "run", "r"),
    (typeof(ResetCliCommand), "reset", "rs"),
    (typeof(CommitCliCommand), "commit", "c"),
    (typeof(ListCliCommand), "list", "ls"),
    (typeof(RemoveCliCommand), "remove", "rm"),
    (typeof(PruneCliCommand), "prune", "pr"),
    (typeof(StopCliCommand), "stop", "s"),
    (typeof(ConfigCliCommand), "config", "cfg")
];

app.Configure(appConfig =>
{
    appConfig.UseAssemblyInformationalVersion();
    foreach (var (commandType, name, alias) in commands)
    {
        var method = typeof(IConfigurator).GetMethod("AddCommand", [typeof(string)]);
        var genericMethod = method?.MakeGenericMethod(commandType);
        var command = genericMethod?.Invoke(appConfig, [name]);
        command?.GetType().GetMethod("WithAlias")?.Invoke(command, [alias]);
    }
});

AnsiConsole.Console = new CustomConsole();

app.Configure(config =>
{
    config.SetExceptionHandler((exception, _) =>
    {
        switch (exception)
        {
            case TimeoutException:
                AnsiConsole.MarkupLine("[red]Timeout exception occurred[/], is the Docker daemon running?");
                return -1;
            default:
                AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
                return -1;
        }
    });
});

return app.Run(args);
