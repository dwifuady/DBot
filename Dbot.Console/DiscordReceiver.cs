﻿using DBot.Console.Entities;
using DBot.Shared;
using DBot.Shared.Configs;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using static DBot.Shared.Request;

namespace DBot.Console;

public class DiscordReceiver : IChatReceiver
{
    private readonly AppConfig? _appConfig;
    private readonly IEnumerable<ICommand> _commands;
    private DiscordSocketClient? _client;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DbotContext _dbotContext;
    private const string ProviderName = "Discord";
    private const int maxMessageLength = 1990; //max lenght is 2000, but we reduce this so we can add something like (1/3) prefix on every message

    public DiscordReceiver(IOptions<AppConfig> options, IEnumerable<ICommand> commands, IHttpClientFactory httpClientFactory, DbotContext dbotContext)
    {
        _appConfig = options?.Value;
        _commands = commands;
        _httpClientFactory = httpClientFactory;
        _dbotContext = dbotContext;
    }

    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        if (_appConfig?.DiscordConfig?.Enable == false)
        {
            Log.Information("{ProviderName} is disabled", ProviderName);
            return;
        }

        if (string.IsNullOrWhiteSpace(_appConfig?.DiscordConfig?.Token))
        {
            Log.Error("Discord token is not set");
            return;
        }

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.DirectMessages |
                            GatewayIntents.MessageContent |
                            GatewayIntents.GuildMembers |
                            GatewayIntents.GuildMessages |
                            GatewayIntents.Guilds |
                            GatewayIntents.GuildIntegrations,
            AlwaysDownloadUsers = false
        };

        _client = new DiscordSocketClient(config);

        _client.Log += DiscordLog;
        _client.Ready += ReadyAsync;

        await _client.LoginAsync(TokenType.Bot, _appConfig?.DiscordConfig?.Token);
        await _client.StartAsync();

        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreatedAsync;

        await Task.Delay(-1, cancellationToken);
    }

    private Task ReadyAsync()
    {
        Log.Information("[{Provider}] {CurrentUser} is connected!", ProviderName, _client?.CurrentUser);

        return Task.CompletedTask;
    }

    private Task DiscordLog(LogMessage logMessage)
    {
        (string message, string propertyValue) message = new ("[{Provider}] " + logMessage.Message + logMessage.Exception ?? "" + logMessage.Severity ?? "" + logMessage.Source ?? "", ProviderName);
        switch(logMessage.Severity)
        {
            case LogSeverity.Warning:
                Log.Warning(message.message, message.propertyValue);
                break;
            case LogSeverity.Error:
                Log.Error(logMessage.Exception, message.message, message.propertyValue);
                break;
            case LogSeverity.Debug:
                Log.Debug(message.message, message.propertyValue);
                break;
            case LogSeverity.Verbose:
                Log.Verbose(message.message, message.propertyValue);
                break;
            default:
                Log.Information(message.message, message.propertyValue);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client?.CurrentUser.Id)
            return;

        if (message.Channel is SocketGuildChannel socketGuildChannel)
        {
            Log.Information("[{Provider}] Received a '{messageText}' message from {user} in chat {ServerName} > {ChannelName}.", ProviderName, message.Content, message.Author.Username, socketGuildChannel.Guild.Name, message.Channel.Name);
        }
        else
        {
            Log.Information("[{Provider}] Received a '{messageText}' message from {user} in chat {ChannelName}.", ProviderName, message.Content, message.Author.Username, message.Channel.Name);
        }

        //if (message.Content == "!ping")
        //{
        //    // Create a new ComponentBuilder, in which dropdowns & buttons can be created.
        //    var cb = new ComponentBuilder()
        //        .WithButton("Click me!", "unique-id", ButtonStyle.Primary);

        //    // Send a message with content 'pong', including a button.
        //    // This button needs to be build by calling .Build() before being passed into the call.
        //    await message.Channel.SendMessageAsync("pong!", components: cb.Build());
        //}

        var request = message
            .Content
            .Parse<Request>();

        IMessage? repliedMsg = null;
        var initialCommand = request.Command;
        Conversation? previousChat = null;
        bool isConversation = false;

        // todo, improve and move this to extensions
        if (message.Reference?.MessageId is not null && await message.Channel.GetMessageAsync(message.Reference.MessageId.Value) is {} referencedMessage)
        {
            if (message.CleanContent.Split(' ')?.Length == 1 && _commands.Any(c => c.AcceptedCommands.Contains(message.CleanContent.ToUpper())))
            {
                request.UpdateArgs(referencedMessage.CleanContent);
            }
            else
            {
                isConversation = true;
                repliedMsg = referencedMessage;
            }
        }

        if (isConversation && repliedMsg is not null)
        {
            previousChat = _dbotContext?.Conversations?.FirstOrDefault(x => x.MessageId == repliedMsg.Id.ToString());
            if (previousChat is not null && !string.IsNullOrWhiteSpace(previousChat.InitialCommand))
            {
                initialCommand = previousChat.InitialCommand;
                request.UpdateCommand(previousChat.InitialCommand);
            }
        }

        var service = _commands?.FirstOrDefault(x => x.AcceptedCommands.Contains(request.Command, StringComparer.OrdinalIgnoreCase));
        if (service is not null)
        {
            var originalMessageId = message.Id.ToString();
            if (isConversation || repliedMsg is null)
            {
                string? parentId = null;
                var messageContent = request.Args;

                if (repliedMsg is not null)
                {
                    originalMessageId = previousChat?.OriginalMessageId;
                    parentId = repliedMsg.Id.ToString();
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
                await _dbotContext!.SaveChangesAsync();

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

            var commandResponse = await service.ExecuteCommand(request);

            switch (commandResponse)
            {
                case ITextResponse textResponse:
                {
                    var messages = textResponse.Message?.Chunk(maxMessageLength)
                            .Select(s => new string(s))
                            .ToList();
                    if (messages?.Any() == true)
                    {
                        var messagesCount = messages.Count;
                        int currentMessage = 1;
                        foreach (var responseMessage in messages)
                        {
                            var prefix = string.Empty;
                            if (messagesCount > 1)
                            {
                                prefix = $"({currentMessage}/{messagesCount}) {Environment.NewLine}";
                            }
                            var sentMessage = await message.Channel.SendMessageAsync(prefix + responseMessage, messageReference: new MessageReference(messageId: message.Id));
                            currentMessage++;

                            if (textResponse?.IsSupportConversation == true && (isConversation || repliedMsg is null))
                            {
                                var botMessage = new Conversation
                                {
                                    InitialCommand = request.Command,
                                    Message = responseMessage,
                                    ParentId = message.Id.ToString(),
                                    IsFromBot = true,
                                    MessageId = sentMessage.Id.ToString(),
                                    OriginalMessageId = originalMessageId
                                };
                                _dbotContext!.Conversations?.Add(botMessage);
                                await _dbotContext!.SaveChangesAsync();
                            }
                        }
                    }
                    break;
                }
                case IFileResponse or IImageResponse:
                {
                    var sourceUrl = string.Empty;
                    if (commandResponse is IFileResponse fileResponse)
                    {
                        sourceUrl = fileResponse.SourceUrl;
                    }
                    else if (commandResponse is IImageResponse imageResponse)
                    {
                        sourceUrl = imageResponse.SourceUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(sourceUrl))
                    {
                        if (!await SendFile(sourceUrl, message))
                        {
                            await message.Channel.SendMessageAsync("Error generating your image, please try again later.", messageReference: new MessageReference(messageId: message.Id));
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Error generating your image, please try again later.", messageReference: new MessageReference(messageId: message.Id));
                    }
                    break;
                }
            }
        }
    }

    private async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        // safety-casting is the best way to prevent something being cast from being null.
        // If this check does not pass, it could not be cast to said type.
        if (interaction is SocketMessageComponent component)
        {
            // Check for the ID created in the button mentioned above.
            if (component.Data.CustomId == "unique-id")
                await interaction.RespondAsync("Thank you for clicking my button!");
            else
                Log.Information("An ID has been received that has no handler!");
        }
    }

    private async Task<bool> SendFile(string fileUrl, SocketMessage message)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponse = await httpClient.GetAsync(fileUrl);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;

            if (!contentType.StartsWith("image/") && !contentType.StartsWith("video/"))
            {
                Log.Error($"[{ProviderName}] Response from {fileUrl} was not an image or video");
                return false;
            }

            var fileStream = await httpResponse.Content.ReadAsStreamAsync();
            var fileExtension = GetFileExtension(contentType);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            await message.Channel.SendFileAsync(new FileAttachment(fileStream, fileName), messageReference: new MessageReference(message.Id));

            return true;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, $"[{ProviderName}] Failed to download file from {fileUrl}");
            throw;
        }
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "video/mp4" => ".mp4",
            // Add support for other file types as needed
            _ => string.Empty,
        };
    }
}