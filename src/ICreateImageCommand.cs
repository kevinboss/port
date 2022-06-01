namespace port;

public interface ICreateImageCommand
{
    Task ExecuteAsync(string imageName, string? tag);

    IObservable<Progress> ProgressObservable { get; }
}