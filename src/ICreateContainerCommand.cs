using Docker.DotNet.Models;

namespace port;

public interface ICreateContainerCommand
{
    Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IEnumerable<string> ports, IList<string> environment);
    Task ExecuteAsync(Container container);
    Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IDictionary<string, IList<PortBinding>> portBindings, IList<string> environment);
}