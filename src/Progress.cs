namespace port;

public class Progress
{
    public Progress(ProgressState progressState, string id, string? description, string? progressMessage)
    {
        Id = id;
        Description = description;
        ProgressMessage = progressMessage;
        ProgressState = progressState;
    }

    public string Id { get; set; }
    public string? Description { get; set; }
    public ProgressState ProgressState { get; set; }
    public string? ProgressMessage { get; set; }
}

public enum ProgressState
{
    Initial,
    Downloading,
    Finished
}