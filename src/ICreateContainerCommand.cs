namespace port;

public interface ICreateContainerCommand
{
    Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tagPrefix, string? tag,
        IEnumerable<string> ports, IList<string> environment);
    Task<string> ExecuteAsync(Container container, string tagPrefix, string newTag);
    Task ExecuteAsync(Container container);
}