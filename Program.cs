using dcma;
using dcma.Commands;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();
registrations.AddSingleton<IAllImagesQuery, AllImagesQuery>();
registrations.AddSingleton<IPromptHelper, PromptHelper>();

var registrar = new dcma.Infrastructure.TypeRegistrar(registrations);

var app = new CommandApp(registrar);

app.Configure(appConfig =>
{
    appConfig.AddCommand<RunCommand>("run");
    appConfig.AddCommand<ListCommand>("list");
    appConfig.AddCommand<CommitCommand>("commit");
});

return app.Run(args);