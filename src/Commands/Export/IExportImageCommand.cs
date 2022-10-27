namespace port.Commands.Export;

internal interface IExportImageCommand
{
    Task ExecuteAsync(string imageId, string path);
}