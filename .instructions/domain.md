# Domain Model Guidelines

## Key Domain Models

### Image

```csharp
public class Image
{
    // Core properties
    public string? Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Tag { get; set; }
    public bool IsSnapshot { get; set; }
    public DateTime? Created { get; set; }
    
    // Relationships
    public ImageGroup Group { get; set; } = null!;
    public string? ParentId { get; set; }
    public Image? Parent { get; set; }
    public IList<Container> Containers { get; set; } = new List<Container>();
    
    // Derived properties
    public bool Running => Containers.Any(container => container is { Running: true });
}
```

### Container

```csharp
public class Container
{
    // Core properties
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Running { get; set; }
    public string ImageId { get; set; } = null!;
    public string? ImageTag { get; set; }
    
    // Methods for container operations
    public string? GetLabel(string key) => /* Get label by key */;
}
```

### ImageGroup

```csharp
public class ImageGroup
{
    public string Name { get; set; } = null!;
    public IList<Image> Images { get; set; } = new List<Image>();
}
```

## Domain Model Principles

1. **Nullable Reference Types**: Use nullable reference types (`string?`) for properties that can be null
2. **Default Values**: Initialize collections in property definitions
3. **Required Properties**: For required non-nullable properties that can't be initialized in the constructor, use the `= null!;` pattern
4. **Derived Properties**: Use expression-bodied members for derived properties

## Domain Model Relationships

- **Image to ImageGroup**: Many-to-One
- **Image to Container**: One-to-Many
- **Image to Parent Image**: Many-to-One

## Helper Methods

Create extension methods or helper classes for common operations on domain models:

```csharp
public static class ImageExtensions
{
    public static bool IsLatest(this Image image) => /* Check if image has latest tag */;
}

// OR

public static class ImageNameHelper
{
    public static string FormatName(string name, string? tag) => /* Format name with tag */;
}
```

## Label Conventions

Docker labels are used to store Port-specific metadata:

```csharp
public static class Constants
{
    public const string TagPrefix = "port.tag-prefix";
    // Other label constants
}
```
