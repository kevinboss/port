using dcma;
using dcma.Commands.Commit;
using dcma.Commands.List;
using dcma.Commands.Pull;
using dcma.Commands.Remove;
using dcma.Commands.Reset;
using dcma.Commands.Run;
using dcma.Config;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();
registrations.AddSingleton<IAllImagesQuery, AllImagesQuery>();
registrations.AddSingleton<IIdentifierPrompt, IdentifierPrompt>();
registrations.AddSingleton<ICreateImageCommand, CreateImageCommand>();
registrations.AddSingleton<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>();
registrations.AddSingleton<IGetImageQuery, GetImageQuery>();
registrations.AddSingleton<IGetContainersQuery, GetContainersQuery>();
registrations.AddSingleton<IGetRunningContainersQuery, GetRunningContainersQuery>();
registrations.AddSingleton<ICreateContainerCommand, CreateContainerCommand>();
registrations.AddSingleton<IRunContainerCommand, RunContainerCommand>();
registrations.AddSingleton<ITerminateContainersCommand, TerminateContainersCommand>();
registrations.AddSingleton<IStopAndRemoveContainerCommand, StopAndRemoveContainerCommand>();
registrations.AddSingleton<IRemoveImageCommand, RemoveImageCommand>();
registrations.AddSingleton<IDownloadImageCommand, DownloadImageCommand>();
registrations.AddSingleton<IIdentifierAndTagEvaluator, IdentifierAndTagEvaluator>();
registrations.AddSingleton(typeof(Config), _ => ConfigFactory.GetOrCreateConfig());
registrations.AddSingleton(typeof(IDockerClient), provider =>
{
    var config = provider.GetService<Config>();
    if (config?.DockerEndpoint == null)
    {
        throw new InvalidOperationException("Docker endpoint has not been configured");
    }

    var endpoint = new Uri(config.DockerEndpoint);
    return new DockerClientConfiguration(endpoint)
        .CreateClient();
});

var registrar = new dcma.Infrastructure.TypeRegistrar(registrations);

var app = new CommandApp(registrar);

app.Configure(appConfig =>
{
    appConfig.AddCommand<RunCommand>("run");
    appConfig.AddCommand<ListCommand>("list");
    appConfig.AddCommand<CommitCommand>("commit");
    appConfig.AddCommand<RemoveCommand>("remove");
    appConfig.AddCommand<PullCommand>("pull");
    appConfig.AddCommand<ResetCommand>("reset");
});

return app.Run(args);