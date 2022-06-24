namespace port;

internal class ContainerIdentifierAndTagEvaluator : IContainerIdentifierAndTagEvaluator
{
    private readonly Config.Config _config;

    public ContainerIdentifierAndTagEvaluator(Config.Config config)
    {
        _config = config;
    }

    public (string identifier, string tag) Evaluate(string imageIdentifier)
    {
        if (ContainerNameHelper.TryGetContainerIdentifierAndTag(imageIdentifier, out var identifierAndTag))
        {
            return (identifierAndTag.identifier, identifierAndTag.tag);
        }

        var imageConfig = _config.GetImageConfigByIdentifier(identifierAndTag.identifier);
        if (imageConfig.ImageTags.Count > 1)
        {
            throw new InvalidOperationException("Given identifier has multiple tags, please manually provide the tag");
        }

        return (imageConfig.Identifier, imageConfig.ImageTags.Single());
    }
}