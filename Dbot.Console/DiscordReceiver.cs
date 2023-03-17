using DBot.Shared;
using DBot.Shared.Configs;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace DBot.Console;

public class DiscordReceiver : IChatReceiver
{
    private readonly AppConfig? _appConfig;
    private readonly IEnumerable<ICommand> _commands;
    private DiscordSocketClient? _client;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string ProviderName = "Discord";

    public DiscordReceiver(IOptions<AppConfig> options, IEnumerable<ICommand> commands, IHttpClientFactory httpClientFactory)
    {
        _appConfig = options?.Value;
        _commands = commands;
        _httpClientFactory = httpClientFactory;
    }

    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_appConfig?.DiscordToken))
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

        await _client.LoginAsync(TokenType.Bot, _appConfig.DiscordToken);
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
        switch(logMessage.Severity)
        {
            case LogSeverity.Warning:
                Log.Warning("[{Provider}] " + logMessage.Message, ProviderName);
                break;
            case LogSeverity.Error:
                Log.Error(logMessage.Exception, "[{Provider}] " + logMessage.Message, ProviderName);
                break;
            case LogSeverity.Debug:
                Log.Debug("[{Provider}] " + logMessage.Message, ProviderName);
                break;
            case LogSeverity.Verbose:
                Log.Verbose("[{Provider}] " + logMessage.Message, ProviderName);
                break;
            default:
                Log.Information("[{Provider}] " + logMessage.Message, ProviderName);
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

        var service = _commands?.FirstOrDefault(x => x.Command.Split('|').Any(c => message.Content.Split(" ")[0].Equals(c, StringComparison.InvariantCultureIgnoreCase)));
        var i = message.Content.IndexOf(" ", StringComparison.Ordinal) + 1;

        if (service is not null)
        {
            var commandResponse = await service.ExecuteCommand(new Request(message.Content.Substring(i)));

            switch (commandResponse)
            {
                case ITextResponse textResponse:
                {
                    await message.Channel.SendMessageAsync(textResponse?.Message, messageReference: new MessageReference(messageId: message.Id));
                    break;
                }
                case IImageResponse imageResponse:
                {
                    if (!string.IsNullOrWhiteSpace(imageResponse?.SourceUrl))
                    {
                        if (!await SendFile(imageResponse.SourceUrl, message))
                        {
                            await message.Channel.SendMessageAsync("I am sorry, i can't find any cute cat for you :(", messageReference: new MessageReference(messageId: message.Id));
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("I am sorry, i can't find any cute cat for you :(", messageReference: new MessageReference(messageId: message.Id));
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

    private async Task<bool> SendFile(string url, SocketMessage message)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var result = await client.GetAsync(url);
            if (!result.IsSuccessStatusCode) return false;

            var content = await result.Content.ReadAsStreamAsync();

            await message.Channel.SendFileAsync(new FileAttachment(content, $@"{Guid.NewGuid()}.jpg"), messageReference: new MessageReference(messageId: message.Id));

            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "[{Provider}] Failed sending image", ProviderName);
            return false;
        }
    }
}