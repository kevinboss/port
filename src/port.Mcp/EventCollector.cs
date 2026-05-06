using port.Orchestrators;

namespace port.Mcp;

internal static class EventCollector
{
    public static (List<string> Messages, IDisposable Subscription) Subscribe(
        IObservable<OrchestrationEvent> events
    )
    {
        var messages = new List<string>();
        var subscription = events.Subscribe(evt =>
        {
            switch (evt)
            {
                case StatusEvent s:
                    messages.Add(s.Message);
                    break;
                case WarningEvent w:
                    messages.Add($"warning: {w.Message}");
                    break;
            }
        });
        return (messages, subscription);
    }
}
