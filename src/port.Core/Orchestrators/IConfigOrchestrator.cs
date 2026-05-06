namespace port.Orchestrators;

public interface IConfigOrchestrator : IOrchestrator
{
    ConfigResult Execute();
}
