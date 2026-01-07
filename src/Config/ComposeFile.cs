namespace port.Config;

public class ComposeFile
{
    public string? Version { get; set; }
    public Dictionary<string, ComposeService> Services { get; set; } = new();
}

public class ComposeService
{
    public string? Image { get; set; }
    public List<string>? Ports { get; set; }
    public List<string>? Environment { get; set; }
}
