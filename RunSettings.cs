using Spectre.Console.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[IMAGEALIAS]")]
    public string? ImageAlias { get; set; }
}