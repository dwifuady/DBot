using DBot.Shared.Configs;
using DBot.Services.HelloWorld;
using DBot.Services.Cat;
using DBot.Services.OpenAI;
using DBot.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

if (environment is not null && environment.Equals("Development", StringComparison.InvariantCultureIgnoreCase))
{
    configurationBuilder.AddUserSecrets<AppConfig>(optional: true, reloadOnChange: true);
}

var configuration = configurationBuilder.Build();

await using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var serviceCollections = new ServiceCollection()
    .AddHelloWorld()
    .AddCat()
    .AddOpenAI();

serviceCollections.AddSingleton<IChatReceiver, TelegramReceiver>();

serviceCollections.Configure<AppConfig>(configuration.GetSection("AppConfig"));
serviceCollections.Configure<OpenAIConfig>(configuration.GetSection("OpenAIConfig"));

var serviceProvider = serviceCollections.BuildServiceProvider();

var receivers = serviceProvider.GetServices<IChatReceiver>();

using CancellationTokenSource cts = new();

var closingEvent = new AutoResetEvent(false);

await Task.Factory.StartNew(async () =>
{
    foreach (var chatReceiver in receivers)
    {
        await chatReceiver.StartReceiving(cts.Token);
    }
});

log.Information("Press Ctrl + C to cancel!");
Console.CancelKeyPress += ((s, a) =>
{
    a.Cancel = true;
    closingEvent.Set();
});

closingEvent.WaitOne();

log.Information("Bot stopped. Bye!");
cts.Cancel();