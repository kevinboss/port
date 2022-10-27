using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Orphan;

internal class OrphanImageCommand : IOrphanImageCommand
{
    private readonly IDockerClient _dockerClient;
    private readonly IProgressSubscriber _progressSubscriber;
    private readonly Subject<Progress> _progressSubject = new();

    public IObservable<Progress> ProgressObservable => _progressSubject.AsObservable();

    public OrphanImageCommand(IDockerClient dockerClient, IProgressSubscriber progressSubscriber)
    {
        _dockerClient = dockerClient;
        _progressSubscriber = progressSubscriber;
    }

    public async Task ExecuteAsync(string imageId)
    {
        var progress = new Progress<JSONMessage>();
        _progressSubscriber.Subscribe(progress, _progressSubject);
        await using var ss = await _dockerClient.Images.SaveImageAsync(imageId);
        await _dockerClient.Images.LoadImageAsync(new ImageLoadParameters
            {
                Quiet = false
            },
            ss,
            progress);
    }
}