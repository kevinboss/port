using Spectre.Console;

namespace port;

public interface IRemoveImagesCliDependentCommand
{
    Task<List<ImageRemovalResult>> ExecuteAsync(List<string> imageIds, StatusContext ctx);
}
