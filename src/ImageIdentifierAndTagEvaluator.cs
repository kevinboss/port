namespace port;

internal class ImageIdentifierAndTagEvaluator(port.Config.Config config) : IImageIdentifierAndTagEvaluator
{
    public (string identifier, string tag) Evaluate(string imageIdentifier)
    {
        if (ImageNameHelper.TryGetImageNameAndTag(imageIdentifier, out var identifierAndTag))
        {
            return (identifierAndTag.imageName, identifierAndTag.tag);
        }

        var imageConfig = config.GetImageConfigByIdentifier(identifierAndTag.imageName);
        return imageConfig.ImageTags.Count > 1
            ? throw new InvalidOperationException("Given identifier has multiple tags, please manually provide the tag")
            : (imageConfig.Identifier, imageConfig.ImageTags.Single());
    }
}
