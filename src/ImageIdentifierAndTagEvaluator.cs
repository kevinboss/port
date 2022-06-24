namespace port;

internal class ImageIdentifierAndTagEvaluator : IImageIdentifierAndTagEvaluator
{
    private readonly Config.Config _config;

    public ImageIdentifierAndTagEvaluator(Config.Config config)
    {
        _config = config;
    }

    public (string identifier, string tag) Evaluate(string imageIdentifier)
    {
        if (ImageNameHelper.TryGetImageNameAndTag(imageIdentifier, out var identifierAndTag))
        {
            return (identifierAndTag.imageName, identifierAndTag.tag);
        }

        var imageConfig = _config.GetImageConfigByIdentifier(identifierAndTag.imageName);
        if (imageConfig.ImageTags.Count > 1)
        {
            throw new InvalidOperationException("Given identifier has multiple tags, please manually provide the tag");
        }

        return (imageConfig.Identifier, imageConfig.ImageTags.Single());
    }
}