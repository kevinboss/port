using dcma;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(appConfig =>
{
    appConfig.AddCommand<RunCommand>("run");
});

return app.Run(args);