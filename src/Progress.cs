namespace port;

public class Progress
{
    internal const string NullId = nameof(NullId);

    internal Progress(Progress progress)
    {
        Id = progress.Id;
        Description = progress.Description;
        ProgressMessage = progress.ProgressMessage;
        ProgressMessage = progress.ProgressMessage;
        CurrentProgress = progress.CurrentProgress;
        ProgressState = progress.ProgressState;
    }

    internal Progress(ProgressState progressState, string id, ProgressSubscriber.TaskSetUpData data)
    {
        Id = id;
        Description = data.Description;
        ProgressMessage = data.ProgressMessage;
        CurrentProgress = data.CurrentProgress;
        TotalProgress = data.TotalProgress;
        ProgressState = progressState;
    }

    public string Id { get; set; }
    public string? Description { get; set; }
    public ProgressState ProgressState { get; set; }
    public string? ProgressMessage { get; set; }
    public long? CurrentProgress { get; set; }
    public long? TotalProgress { get; set; }
}

public enum ProgressState
{
    Initial,
    Downloading,
    Finished,
}
