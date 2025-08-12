using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class CreateImageCommand : ICreateImageCommand
{
    private readonly IDockerClient _dockerClient;
    private readonly IProgressSubscriber _progressSubscriber;
    private readonly Subject<Progress> _progressSubject = new();

    public IObservable<Progress> ProgressObservable => _progressSubject.AsObservable();

    public CreateImageCommand(IDockerClient dockerClient, IProgressSubscriber progressSubscriber)
    {
        _dockerClient = dockerClient;
        _progressSubscriber = progressSubscriber;
    }

    public async Task ExecuteAsync(string imageName, string? tag)
    {
        var progress = new Progress<JSONMessage>();

        _progressSubscriber.Subscribe(progress, _progressSubject);
        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = imageName, Tag = tag },
            null,
            progress
        );
    }
}
