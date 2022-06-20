using System.Collections.ObjectModel;

namespace port;

public class ImageGroup
{
    private Collection<Image> _images = new();

    public ImageGroup(string identifier)
    {
        Identifier = identifier;
    }

    public string Identifier { get; set; }

    public IReadOnlyCollection<Image> Images
    {
        get => _images;
    }

    public void AddImage(Image image)
    {
        image.Group = this;
        _images.Add(image);
    }
}