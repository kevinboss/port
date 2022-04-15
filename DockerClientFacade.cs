using Docker.DotNet.Models;

namespace dcma;

public static class DockerClientFacade
{
    public static async Task<ContainerListResponse?> GetContainerAsync(string containerName)
    {
        var containerListResponses = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", new Dictionary<string, bool>
                        {
                            { $"/{containerName}", true }
                        }
                    }
                }
            });
        return containerListResponses.SingleOrDefault(e => e.Names.Any(name => name == $"/{containerName}"));
    }

    public static async Task TerminateContainers(IEnumerable<string> containerNames)
    {
        var containerNameParams = containerNames.ToDictionary(s => s, _ => true);
        var containers = await Services.DockerClient.Value.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = long.MaxValue,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", containerNameParams
                    }
                }
            });

        foreach (var containerListResponse in containers)
        {
            await Services.DockerClient.Value.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
        }
    }
}