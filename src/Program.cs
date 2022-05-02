using dcma;
using dcma.Commit;
using dcma.List;
using dcma.Run;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();
registrations.AddSingleton<IAllImagesQuery, AllImagesQuery>();
registrations.AddSingleton<IPromptHelper, PromptHelper>();
registrations.AddSingleton<ICreateImageCommand, CreateImageCommand>();
registrations.AddSingleton<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>();
registrations.AddSingleton<IGetImageQuery, GetImageQuery>();
registrations.AddSingleton<IGetContainerQuery, GetContainerQuery>();
registrations.AddSingleton<IGetRunningContainersQuery, GetRunningContainersQuery>();
registrations.AddSingleton<ICreateContainerCommand, CreateContainerCommand>();
registrations.AddSingleton<IRunContainerCommand, RunContainerCommand>();
registrations.AddSingleton<ITerminateContainersCommand, TerminateContainersCommand>();
registrations.AddSingleton(typeof(IDockerClient), provider =>
{
    if (Services.Config.Value.DockerEndpoint == null)
    {
        throw new InvalidOperationException("Docker endpoint has not been configured");
    }

    var endpoint = new Uri(Services.Config.Value.DockerEndpoint);
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
});

return app.Run(args);