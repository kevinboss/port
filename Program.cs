using dcma;
using dcma.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(appConfig =>
{
    appConfig.AddCommand<RunCommand>("run");
    appConfig.AddCommand<ListCommand>("list");
    appConfig.AddCommand<CommitCommand>("commit");
});

return app.Run(args);