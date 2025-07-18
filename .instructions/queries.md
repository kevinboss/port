# Query Implementation Guidelines

## Query Interface Pattern

When defining a new query interface:

```csharp
namespace port;

public interface INewDataQuery
{
    // For single-item queries
    Task<ResultType> QueryAsync(Parameters parameters);
    
    // For collection queries
    IAsyncEnumerable<ResultType> QueryAsync(Parameters parameters);
}
```

## Query Implementation Pattern

When implementing a query:

```csharp
namespace port;

public class NewDataQuery(IDockerClient dockerClient) : INewDataQuery
{
    public async Task<ResultType> QueryAsync(Parameters parameters)
    {
        // Implementation using dockerClient
        // Apply any filtering or mapping of Docker API responses to domain models
        return result;
    }
    
    // OR for collection queries
    public async IAsyncEnumerable<ResultType> QueryAsync(Parameters parameters)
    {
        var results = await dockerClient.SomeCollection.ListAsync(parameters);
        
        foreach (var item in results)
        {
            yield return MapToDomainModel(item);
        }
    }
}
```

## Query Naming Conventions

- Query interfaces should be named `I[Subject]Query`
- Query implementations should be named `[Subject]Query`
- Examples: `IAllImagesQuery`/`AllImagesQuery`, `IGetImageQuery`/`GetImageQuery`

## Service Registration

Register new query services in Program.cs:

```csharp
registrations.AddTransient<INewDataQuery, NewDataQuery>();
```

## Error Handling in Queries

Queries should generally propagate exceptions to be handled by the calling code, but can handle known Docker API issues:

```csharp
try
{
    return await dockerClient.SomeResource.GetAsync(parameters);
}
catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // For "not found" cases, either return null/empty or throw a specific exception
    return null; // or return empty collection, or throw domain-specific exception
}
```

## Filtering and Mapping

- Docker API responses should be mapped to domain models
- Apply filtering logic in the query rather than in the calling code
- Use LINQ and async LINQ operators for filtering collections

```csharp
public async IAsyncEnumerable<Image> QueryAsync()
{
    var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters
    {
        All = true
    });

    foreach (var image in images)
    {
        if (ShouldIncludeImage(image))
        {
            yield return MapToImage(image);
        }
    }
}
```
