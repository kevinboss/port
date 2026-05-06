using System.Reactive.Subjects;
using port.Config;

namespace port.Orchestrators;

public class ConfigOrchestrator : IConfigOrchestrator
{
    private readonly Subject<OrchestrationEvent> _events = new();

    public IObservable<OrchestrationEvent> Events => _events;

    public ConfigResult Execute()
    {
        ConfigFactory.GetOrCreateConfig();
        var path = ConfigFactory.GetConfigFilePath();
        return new ConfigResult(path);
    }
}
