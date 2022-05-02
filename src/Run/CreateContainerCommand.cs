using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Run;

internal class CreateContainerCommand : ICreateContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string identifier, string imageName, string tag, int portFrom, int portTo)
    {
        return _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = $"{identifier}.{tag}",
            Image = DockerHelper.JoinImageNameAndTag(imageName, tag),
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        portFrom.ToString(), new List<PortBinding>
                        {
                            new()
                            {
                                HostPort = portTo.ToString()
                            }
                        }
                    }
                }
            }
        });
    }
}