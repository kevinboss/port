using Docker.DotNet.Models;

namespace port;

internal interface IProgressSubscriber
{
    IDisposable Subscribe(Progress<JSONMessage> progress, IObserver<Progress> observer);
}