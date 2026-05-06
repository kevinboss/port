using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using port.Commands.Commit;
using port.Config;
using port.Orchestrators;

namespace port.Mcp;

public static class McpHost
{
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
            options.LogToStandardErrorThreshold = LogLevel.Trace
        );

        RegisterPortServices(builder.Services);

        builder
            .Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(PortMcpTools).Assembly);

        await builder.Build().RunAsync(cancellationToken);
    }

    private static void RegisterPortServices(IServiceCollection services)
    {
        services.AddTransient<IAllImagesQuery, AllImagesQuery>();
        services.AddTransient<IGetDigestsByIdQuery, GetDigestsByIdQuery>();
        services.AddTransient<ICreateImageCommand, CreateImageCommand>();
        services.AddTransient<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>();
        services.AddTransient<IGetImageQuery, GetImageQuery>();
        services.AddTransient<IGetImageIdQuery, GetImageIdQuery>();
        services.AddTransient<IDoesImageExistQuery, DoesImageExistQuery>();
        services.AddTransient<IGetContainersQuery, GetContainersQuery>();
        services.AddTransient<IGetRunningContainersQuery, GetRunningContainersQuery>();
        services.AddTransient<ICreateContainerCommand, CreateContainerCommand>();
        services.AddTransient<IRunContainerCommand, RunContainerCommand>();
        services.AddTransient<IStopContainerCommand, StopContainerCommand>();
        services.AddTransient<IStopAndRemoveContainerCommand, StopAndRemoveContainerCommand>();
        services.AddTransient<IRenameContainerCommand, RenameContainerCommand>();
        services.AddTransient<IRemoveImageCommand, RemoveImageCommand>();
        services.AddTransient<IRemoveImagesCommand, RemoveImagesCommand>();
        services.AddTransient<IImageIdentifierAndTagEvaluator, ImageIdentifierAndTagEvaluator>();
        services.AddTransient<IProgressSubscriber, ProgressSubscriber>();

        services.AddTransient<IRunOrchestrator, RunOrchestrator>();
        services.AddTransient<IStopOrchestrator, StopOrchestrator>();
        services.AddTransient<IResetOrchestrator, ResetOrchestrator>();
        services.AddTransient<ICommitOrchestrator, CommitOrchestrator>();
        services.AddTransient<IPullOrchestrator, PullOrchestrator>();
        services.AddTransient<IRemoveOrchestrator, RemoveOrchestrator>();
        services.AddTransient<IPruneOrchestrator, PruneOrchestrator>();
        services.AddTransient<IListOrchestrator, ListOrchestrator>();
        services.AddTransient<IConfigOrchestrator, ConfigOrchestrator>();

        services.AddSingleton(_ => ConfigFactory.GetOrCreateConfig());
        services.AddSingleton<IDockerClient>(provider =>
        {
            var config = provider.GetRequiredService<port.Config.Config>();
            if (config.DockerEndpoint == null)
                throw new InvalidOperationException("Docker endpoint has not been configured");
            return new DockerClientConfiguration(
                new Uri(config.DockerEndpoint),
                null,
                TimeSpan.FromSeconds(300)
            ).CreateClient();
        });
    }
}
