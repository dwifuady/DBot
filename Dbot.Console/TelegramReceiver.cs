using DBot.Shared;
using DBot.Shared.Configs;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using static DBot.Shared.Request;
using DBot.Console.Entities;

namespace DBot.Console;

public class TelegramReceiver : IChatReceiver
{
    private readonly AppConfig? _appConfig;
    private readonly IEnumerable<ICommand> _commands;
    private readonly DbotContext _dbotContext;
    private const string ProviderName = "Telegram";
    private long botId;

    public TelegramReceiver(IOptions<AppConfig> options, IEnumerable<ICommand> commands, DbotContext dbotContext)
    {
        _appConfig = options?.Value;
        _commands = commands;
        _dbotContext = dbotContext;
    }

    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        if (_appConfig?.TelegramConfig?.Enable == false)
        {
            Log.Information("{ProviderName} is disabled", ProviderName);
            return;
        }

        if (string.IsNullOrWhiteSpace(_appConfig?.TelegramConfig?.Token))
        {
            Log.Error("[{Provider}] Telegram token is not set", ProviderName);
            return;
        }

        var botClient = new TelegramBotClient(_appConfig.TelegramConfig.Token);

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
        botId = me.Id;
        Log.Information("[{Provider}] {CurrentUser} is connected!", ProviderName, me.Username);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { Text: { } messageText } message)
            return;
        // Only process text messages

        if (GetSender(message) == Sender.Bot)
            return;

        var chatId = message.Chat.Id;

        var request = messageText
            .Parse<Request>();

        Message? repliedMsg = null;
        var initialCommand = request.Command;
        Conversation? previousChat = null;
        bool isConversation = false;

        if (message.ReplyToMessage is { Text: { } repliedMessageText} repliedMessage)
        {
            if (messageText.Split(' ')?.Length == 1 && _commands.Any(c => c.AcceptedCommands.Contains(messageText.ToUpper())))
            {
                request.UpdateArgs(repliedMessageText);
            }
            else
            {
                isConversation = true;
            }
            repliedMsg = repliedMessage;
        }
        if (isConversation && repliedMsg is not null)
        {
            previousChat = _dbotContext?.Conversations?.FirstOrDefault(x => x.MessageId == repliedMsg.MessageId.ToString());
            if (previousChat is not null && !string.IsNullOrWhiteSpace(previousChat.InitialCommand))
            {
                initialCommand = previousChat.InitialCommand;
                request.UpdateCommand(previousChat.InitialCommand);
            }
        }

        var service = _commands?.FirstOrDefault(x => x.AcceptedCommands.Contains(initialCommand, StringComparer.OrdinalIgnoreCase));

        if (service is not null)
        {
            var originalMessageId = message.MessageId.ToString();
            if (isConversation || repliedMsg is null)
            {
                string? parentId = null;
                var messageContent = request.Args;

                if (repliedMsg is not null)
                {
                    originalMessageId = previousChat?.OriginalMessageId;
                    parentId = repliedMsg.MessageId.ToString();
                    messageContent = request.FullArgs;
                }

                var conversation = new Conversation
                {
                    InitialCommand = initialCommand,
                    Message = messageContent,
                    ParentId = parentId,
                    IsFromBot = false,
                    MessageId = originalMessageId,
                    OriginalMessageId = originalMessageId
                };
                _dbotContext!.Conversations?.Add(conversation);
                await _dbotContext!.SaveChangesAsync(cancellationToken);

                var conversations = _dbotContext?.Conversations?.ToList();

                if (conversations?.Any() == true)
                {
                    var messageChain = new List<RequestMessage>();
                    var sequence = 1;
                    foreach (var conver in conversations.Where(x => x.OriginalMessageId == conversation.OriginalMessageId).OrderBy(x => x.Id))
                    {
                        var sender = conver.IsFromBot ? Sender.Bot : Sender.User;
                        var msg = new RequestMessage(sender, conver.Message!, sequence, conver.InitialCommand!);
                        messageChain.Add(msg);
                        sequence++;
                    }

                    request.UpdateMessageChain(messageChain);
                }
            }

            Log.Information("[{Provider}] Received a '{messageText}' message from {user} in chat {chatId}.", ProviderName, messageText, message.From?.Username , chatId);
            var commandResponse = await service.ExecuteCommand(request);

            switch (commandResponse)
            {
                case ITextResponse textResponse:
                    {
                        var sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: textResponse?.Message ?? "",
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: message.MessageId,
                            cancellationToken: cancellationToken);

                        if (textResponse?.IsSupportConversation == true && (isConversation || repliedMsg is null))
                        {
                            var botMessage = new Conversation
                            {
                                InitialCommand = request.Command,
                                Message = textResponse?.Message,
                                ParentId = message.MessageId.ToString(),
                                IsFromBot = true,
                                MessageId = sentMessage.MessageId.ToString(),
                                OriginalMessageId = originalMessageId
                            };
                            _dbotContext!.Conversations?.Add(botMessage);
                            await _dbotContext!.SaveChangesAsync(cancellationToken);
                        }
                        break;
                    }
                case IImageResponse imageResponse:
                    {
                        var sentMessage = await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: imageResponse.SourceUrl,
                            caption: imageResponse.Caption,
                            parseMode: ParseMode.Html,
                            replyToMessageId: message.MessageId,
                            cancellationToken: cancellationToken);
                        break;
                    }
                case IFileResponse fileResponse:
                {
                    if (fileResponse.SourceUrl.EndsWith(".jpg"))
                    {
                        var sentMessage = await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: fileResponse.SourceUrl,
                                caption: fileResponse.Caption,
                                parseMode: ParseMode.Html,
                                replyToMessageId: message.MessageId,
                                cancellationToken: cancellationToken);
                    }
                    else if (fileResponse.SourceUrl.EndsWith(".mp4"))
                    {
                        var sentMessage = await botClient.SendVideoAsync(
                                chatId: chatId,
                                video: fileResponse.SourceUrl,
                                caption: fileResponse.Caption,
                                parseMode: ParseMode.Html,
                                replyToMessageId: message.MessageId,
                                cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var sentMessage = await botClient.SendDocumentAsync(
                                chatId: chatId,
                                document: fileResponse.SourceUrl,
                                caption: fileResponse.Caption,
                                parseMode: ParseMode.Html,
                                replyToMessageId: message.MessageId,
                                cancellationToken: cancellationToken);
                    }
                    break;
                }
            }
        }
    }

    private Sender GetSender(Message message)
    {
        return message?.From?.Id == botId ? Sender.Bot : Sender.User;
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