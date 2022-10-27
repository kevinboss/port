using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Import;

internal class ImportImageCommand : IImportImageCommand
{
    private readonly IDockerClient _dockerClient;
    private readonly IProgressSubscriber _progressSubscriber;
    private readonly Subject<Progress> _progressSubject = new();

    public IObservable<Progress> ProgressObservable => _progressSubject.AsObservable();

    public ImportImageCommand(IDockerClient dockerClient, IProgressSubscriber progressSubscriber)
    {
        _dockerClient = dockerClient;
        _progressSubscriber = progressSubscriber;
    }

    public async Task ExecuteAsync(string path, string imageName, string tag)
    {
        var progress = new Progress<JSONMessage>();
        _progressSubject.Subscribe(progress1 => { });
        _progressSubscriber.Subscribe(progress, _progressSubject);
        await using var ss = File.OpenRead(path);
        await _dockerClient.Images.LoadImageAsync(new ImageLoadParameters
            {
                Quiet = false
            },
            ss,
            progress);
    }
}