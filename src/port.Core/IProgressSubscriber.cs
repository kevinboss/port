using Docker.DotNet.Models;

namespace port;

public interface IProgressSubscriber
{
    IDisposable Subscribe(Progress<JSONMessage> progress, IObserver<Progress> observer);
}
