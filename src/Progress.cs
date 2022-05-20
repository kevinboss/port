namespace port;

public class Progress
{
    public Progress(bool initial, string id, string description, string? progressMessage)
    {
        Initial = initial;
        Id = id;
        Description = description;
        ProgressMessage = progressMessage;
    }

    public bool Initial { get; set; }
    public string Id { get; set; }
    public string Description { get; set; }
    public string? ProgressMessage { get; set; }
}