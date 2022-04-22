namespace dcma;

public static class DockerHelper
{
    public static (string ImageName, string? Tag) GetImageNameAndTag(string imageName)
    {
        var idx = imageName.LastIndexOf(':');
        return idx != -1 ? (imageName[..idx], imageName[(idx + 1)..]) : (imageName, null);
    }
}