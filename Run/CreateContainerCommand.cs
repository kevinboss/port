using Docker.DotNet.Models;

namespace dcma.Run;

internal class CreateContainerCommand : ICreateContainerCommand
{
    public Task ExecuteAsync(string identifier, string imageName, string tag, int portFrom, int portTo)
    {
        return Services.DockerClient.Value.Containers.CreateContainerAsync(new CreateContainerParameters
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