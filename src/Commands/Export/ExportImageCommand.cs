using Docker.DotNet;

namespace port.Commands.Export;

internal class ExportImageCommand : IExportImageCommand
{
    private readonly IDockerClient _dockerClient;

    public ExportImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string imageId, string path)
    {
        await using var ss = await _dockerClient.Images.SaveImageAsync(imageId);
        await using var fs = new FileStream(path, FileMode.Create);
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await ss.ReadAsync(buffer)) > 0)
        {
            fs.Write(buffer, 0, bytesRead);
        }
        fs.Position = 0;
    }
}