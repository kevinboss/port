namespace port;

public class ImageRemovalResult
{
    public ImageRemovalResult(string imageId, bool successful)
    {
        ImageId = imageId;
        Successful = successful;
    }

    public string ImageId { get; }
    public bool Successful { get; }
}
