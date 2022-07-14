using Docker.DotNet.Models;

namespace port;

public interface ICreateContainerCommand
{
    Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag, IEnumerable<string> ports);
    Task ExecuteAsync(Container container);
    Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IList<Port> containerPorts);
}