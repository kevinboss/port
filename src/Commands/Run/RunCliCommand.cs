using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Run;

internal class RunCliCommand(
    IImageIdentifierPrompt imageIdentifierPrompt,
    ICreateImageCliChildCommand createImageCliChildCommand,
    IGetImageQuery getImageQuery,
    IGetContainersQuery getContainersQuery,
    ICreateContainerCommand createContainerCommand,
    IRunContainerCommand runContainerCommand,
    IStopContainerCommand stopContainerCommand,
    port.Config.Config config,
    IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
    IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
    ListCliCommand listCliCommand)
    : AsyncCommand<RunSettings>
{
    private const char PortSeparator = ':';

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not launch untagged image");
        
        await TerminateOtherContainersAsync(identifier);
        await LaunchImageAsync(identifier, tag, settings.Reset);

        await listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await imageIdentifierPrompt.GetRunnableIdentifierAndTagFromUserAsync("run");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private Task TerminateOtherContainersAsync(string identifier)
    {
        var imageConfig = config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var hostPorts = imageConfig.Ports
            .Select(e => e.Split(PortSeparator)[0])
            .ToList();
        var spinnerTex = $"Terminating containers using host ports '{string.Join(", ", hostPorts)}'";
        return Spinner.StartAsync(spinnerTex, async _ =>
        {
            var containers = GetRunningContainersUsingHostPortsAsync(hostPorts);
            await foreach (var container in containers)
                await stopContainerCommand.ExecuteAsync(container.Id);
        });
    }

    private IAsyncEnumerable<Container> GetRunningContainersUsingHostPortsAsync(
        IEnumerable<string> hostPorts)
    {
        return getContainersQuery.QueryRunningAsync()
            .Where(container =>
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (container.PortBindings is null) return false;
                var usedHostPorts = container.PortBindings
                    .SelectMany(pb => pb.Value
                        .Select(hp => hp.HostPort));
                return container.PortBindings
                    .Any(p => { return hostPorts.Any(p => usedHostPorts.Contains(p)); });
            });
    }

    private async Task LaunchImageAsync(string identifier, string tag, bool resetContainer)
    {
        var constructedImageName = ImageNameHelper.BuildImageName(identifier, tag);
        var imageConfig = config.GetImageConfigByIdentifier(identifier);


        var imageName = imageConfig.ImageName;
        var containerName = ContainerNameHelper.BuildContainerName(identifier, tag);
        var existingImage = await Spinner.StartAsync($"Query existing image: {constructedImageName}",
            async _ =>
            {
                if (imageConfig.ImageTags.Contains(tag)) return await getImageQuery.QueryAsync(imageName, tag);
                var existingImage = await getImageQuery.QueryAsync(imageName, tag);
                if (existingImage is not null) return existingImage;
                tag = $"{TagPrefixHelper.GetTagPrefix(identifier)}{tag}";
                return await getImageQuery.QueryAsync(imageName, tag);
            });
        if (existingImage is null)
        {
            await createImageCliChildCommand.ExecuteAsync(imageName, tag);
            existingImage = await Spinner.StartAsync($"Re-query existing image: {constructedImageName}",
                async _ => await getImageQuery.QueryAsync(imageName, tag));
        }

        var tagPrefix = existingImage?.GetLabel(Constants.TagPrefix);

        await Spinner.StartAsync($"Launching {constructedImageName}", async _ =>
        {
            var containers = await getContainersQuery.QueryByContainerNameAsync(containerName).ToListAsync();
            var ports = imageConfig.Ports;
            var environment = imageConfig.Environment;
            switch (containers.Count)
            {
                case 1 when resetContainer:
                    await stopAndRemoveContainerCommand.ExecuteAsync(containers.Single().Id);
                    containerName =
                        await createContainerCommand.ExecuteAsync(identifier, imageName, tagPrefix, tag, ports,
                            environment);
                    break;
                case 1 when !resetContainer:
                    break;
                case 0:
                    containerName =
                        await createContainerCommand.ExecuteAsync(identifier, imageName, tagPrefix, tag, ports,
                            environment);
                    break;
            }

            await runContainerCommand.ExecuteAsync(containerName);
        });
    }
}