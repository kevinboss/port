using Spectre.Console;

namespace port;

internal interface IRemoveImagesCliDependentCommand
{
    Task<List<ImageRemovalResult>> ExecuteAsync(List<string> imageIds, StatusContext ctx);
}
