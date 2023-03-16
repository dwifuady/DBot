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
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

if (environment is not null && environment.Equals("Development", StringComparison.InvariantCultureIgnoreCase))
{
    configurationBuilder.AddUserSecrets<AppConfig>(optional: true, reloadOnChange: true);
}

var configuration = configurationBuilder.Build();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

// Bot Services
var serviceCollections = new ServiceCollection()
    .AddHelloWorld()
    .AddCat()
    .AddOpenAI();

// Chat Receiver
serviceCollections.AddSingleton<IChatReceiver, TelegramReceiver>();
serviceCollections.AddSingleton<IChatReceiver, DiscordReceiver>();

// Config
serviceCollections.Configure<AppConfig>(configuration.GetSection("AppConfig"));
serviceCollections.Configure<OpenAIConfig>(configuration.GetSection("OpenAIConfig"));

var serviceProvider = serviceCollections.BuildServiceProvider();

using CancellationTokenSource cts = new();
var closingEvent = new AutoResetEvent(false);

await Task.Factory.StartNew(async () =>
{
    var receivers = serviceProvider.GetServices<IChatReceiver>();
    foreach (var chatReceiver in receivers)
    {
        await chatReceiver.StartReceiving(cts.Token);
    }
});

Console.CancelKeyPress += ((s, a) =>
{
    a.Cancel = true;
    closingEvent.Set();
});

closingEvent.WaitOne();

Log.Information("Bot stopped. Bye!");
cts.Cancel();