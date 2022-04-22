namespace dcma;

public static class DockerHelper
{
    public static (string imageName, string tag) GetImageNameAndTag(string imageName)
    {
        var idx = imageName.LastIndexOf(':');
        if (idx == -1)
        {
            throw new InvalidOperationException();
        }
        return (imageName[..idx], imageName[(idx + 1)..]);
    }

    public static string JoinImageNameAndTag(string imageName, string? tag)
    {
        return tag == null ? imageName : $"{imageName}:{tag}";
    }
}