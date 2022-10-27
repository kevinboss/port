namespace port.Commands.Orphan;

internal interface IOrphanImageCommand
{
    Task ExecuteAsync(string imageId);
    IObservable<Progress> ProgressObservable { get; }
}