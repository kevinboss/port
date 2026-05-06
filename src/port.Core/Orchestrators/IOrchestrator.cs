namespace port.Orchestrators;

public interface IOrchestrator
{
    IObservable<OrchestrationEvent> Events { get; }
}
