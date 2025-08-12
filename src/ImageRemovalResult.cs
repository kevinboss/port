namespace port;

internal class ImageRemovalResult
{
    public ImageRemovalResult(string imageId, bool successful)
    {
        ImageId = imageId;
        Successful = successful;
    }

    internal string ImageId { get; }
    internal bool Successful { get; }
}
