using Spectre.Console;

namespace port;

internal class RemoveImagesCliDependentCommand : IRemoveImagesCliDependentCommand
{
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;

    public RemoveImagesCliDependentCommand(IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, 
        IRemoveImageCommand removeImageCommand)
    {
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
    }

    public async Task<List<ImageRemovalResult>> ExecuteAsync(List<string> imageIds, StatusContext ctx)
    {
        var result = new List<ImageRemovalResult>();
        foreach (var imageId in imageIds)
        {
            var containers = await _getContainersQuery.QueryByImageIdAsync(imageId).ToListAsync();
            ctx.Status = $"Removing containers using '{imageId}'".EscapeMarkup();
            foreach (var container in containers)
            {
                await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            }

            ctx.Status = $"Containers using '{imageId}' removed".EscapeMarkup();

            result.Add(await _removeImageCommand.ExecuteAsync(imageId));
        }

        return result;
    }
}