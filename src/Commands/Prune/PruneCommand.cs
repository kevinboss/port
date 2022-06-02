using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

public class PruneCommand : AsyncCommand
{
    private readonly IDockerClient _dockerClient;

    public PruneCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        await _dockerClient.Images.PruneImagesAsync(new ImagesPruneParameters());
        return 0;
    }
}
