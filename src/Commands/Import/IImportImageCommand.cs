namespace port.Commands.Import;

internal interface IImportImageCommand
{
    Task ExecuteAsync(string path, string imageName, string tag);
}