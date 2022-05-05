using dcma.Config;

namespace dcma;

internal class IdentifierAndTagEvaluator : IIdentifierAndTagEvaluator
{
    private IConfig _config;

    public IdentifierAndTagEvaluator(IConfig config)
    {
        _config = config;
    }

    public (string identifier, string tag) Evaluate(string imageIdentifier)
    {
        if (DockerHelper.TryGetImageNameAndTag(imageIdentifier, out var identifierAndTag))
        {
            return (identifierAndTag.imageName, identifierAndTag.tag);
        }

        var imageConfig = _config.GetImageConfigByIdentifier(identifierAndTag.imageName);
        return (imageConfig.Identifier, imageConfig.ImageTag);
    }
}