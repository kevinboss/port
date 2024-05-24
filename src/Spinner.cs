using Spectre.Console;

namespace port;

internal static class Spinner
{
    public static T Start<T>(string status, Func<StatusContext, T> func)
    {
        return AnsiConsole.Status().Spinner(global::Spectre.Console.Spinner.Known.Dots).Start(status, func);
    }

    public static Task StartAsync(string status, Func<StatusContext, Task> action)
    {
        return AnsiConsole.Status().Spinner(global::Spectre.Console.Spinner.Known.Dots).StartAsync(status, action);
    }

    public static Task<T> StartAsync<T>(string status, Func<StatusContext, Task<T>> func)
    {
        return AnsiConsole.Status().Spinner(global::Spectre.Console.Spinner.Known.Dots).StartAsync(status, func);
    }
}