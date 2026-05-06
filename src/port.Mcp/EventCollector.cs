using Docker.DotNet;
using ModelContextProtocol;
using port.Orchestrators;

namespace port.Mcp;

internal static class EventCollector
{
    public static async Task<McpToolResponse<T>> InvokeAsync<T>(
        IObservable<OrchestrationEvent> events,
        Func<Task<T>> work
    )
    {
        var messages = new List<string>();
        using var subscription = events.Subscribe(evt =>
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

        try
        {
            return new McpToolResponse<T>(await work(), messages);
        }
        catch (Exception e) when (IsDomainException(e))
        {
            throw new McpException(e.Message, e);
        }
    }

    public static async Task<T> InvokeAsync<T>(Func<Task<T>> work)
    {
        try
        {
            return await work();
        }
        catch (Exception e) when (IsDomainException(e))
        {
            throw new McpException(e.Message, e);
        }
    }

    public static T Invoke<T>(Func<T> work)
    {
        try
        {
            return work();
        }
        catch (Exception e) when (IsDomainException(e))
        {
            throw new McpException(e.Message, e);
        }
    }

    private static bool IsDomainException(Exception e) =>
        e is InvalidOperationException or ArgumentException or DockerApiException;
}
