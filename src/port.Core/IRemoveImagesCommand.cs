using port.Orchestrators;

namespace port;

public interface IRemoveImagesCommand
{
    Task<List<ImageRemovalResult>> ExecuteAsync(
        List<string> imageIds,
        IObserver<OrchestrationEvent>? events = null,
        CancellationToken ct = default
    );
}
