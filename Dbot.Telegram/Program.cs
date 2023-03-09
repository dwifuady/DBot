using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DBot.Shared;
using DBot.Shared.Configs;
using DBot.Services.HelloWorld;
using DBot.Services.Cat;
using DBot.Services.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Dbot.Telegram;
using Microsoft.Extensions.Options;
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

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var serviceCollections = new ServiceCollection()
    .AddHelloWorld()
    .AddCat()
    .AddOpenAI();
    
serviceCollections.Configure<AppConfig>(configuration.GetSection("AppConfig"));
serviceCollections.Configure<OpenAIConfig>(configuration.GetSection("OpenAIConfig"));

var serviceProvider = serviceCollections.BuildServiceProvider();

var option = serviceProvider.GetService<IOptions<AppConfig>>()?.Value;

if (string.IsNullOrWhiteSpace(option?.TelegramToken))
{
    log.Error("Telegram token is not set");
    return;
}

var botClient = new TelegramBotClient(option.TelegramToken);

var services = serviceProvider.GetServices<ICommand>();

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

log.Information("Start listening for {UserName}", me.Username);
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    log.Information("Received a '{messageText}' message in chat {chatId}.", messageText, chatId);

    var service = services?.FirstOrDefault(x => x.Command.Split('|').Any(c => messageText.Split(" ")[0].Equals(c, StringComparison.InvariantCultureIgnoreCase)));
    var i = messageText.IndexOf(" ") + 1;
    
    if (service is not null)
    {
        var commandResponse = await service.ExecuteCommand(new Request(messageText.Substring(i)));

        if (commandResponse is null)
        {
            return;
        }

        if (commandResponse is ITextResponse textResponse)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: textResponse?.Message ?? "",
                cancellationToken: cancellationToken);
        }
        else if (commandResponse is IImageResponse imageResponse)
        {
            Message sentMessage = await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: imageResponse.SourceUrl,
                caption: imageResponse.Caption,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
    }

}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    log.Error(ErrorMessage);
    return Task.CompletedTask;
}