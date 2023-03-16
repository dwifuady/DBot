using DBot.Shared;
using DBot.Shared.Configs;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;

namespace DBot.Console;

public class TelegramReceiver : IChatReceiver
{
    private readonly AppConfig? _appConfig;
    private readonly IEnumerable<ICommand> _commands;
    public TelegramReceiver(IOptions<AppConfig> options, IEnumerable<ICommand> commands)
    {
        _appConfig = options?.Value;
        _commands = commands;
    }

    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_appConfig?.TelegramToken))
        {
            Log.Error("Telegram token is not set");
            return;
        }

        var botClient = new TelegramBotClient(_appConfig.TelegramToken);

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

        var me = await botClient.GetMeAsync(cancellationToken);

        Log.Information("[Telegram] Start listening for {UserName}", me.Username);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { Text: { } messageText } message)
            return;
        // Only process text messages

        var chatId = message.Chat.Id;

        Log.Information("[Telegram] Received a '{messageText}' message in chat {chatId}.", messageText, chatId);

        var service = _commands?.FirstOrDefault(x => x.Command.Split('|').Any(c => messageText.Split(" ")[0].Equals(c, StringComparison.InvariantCultureIgnoreCase)));
        var i = messageText.IndexOf(" ", StringComparison.Ordinal) + 1;

        if (service is not null)
        {
            var commandResponse = await service.ExecuteCommand(new Request(messageText.Substring(i)));

            switch (commandResponse)
            {
                case ITextResponse textResponse:
                    {
                        var sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: textResponse?.Message ?? "",
                            cancellationToken: cancellationToken);
                        break;
                    }
                case IImageResponse imageResponse:
                    {
                        var sentMessage = await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: imageResponse.SourceUrl,
                            caption: imageResponse.Caption,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken
                        );
                        break;
                    }
            }
        }

    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.Error(errorMessage);
        return Task.CompletedTask;
    }

}