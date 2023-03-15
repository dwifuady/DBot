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

    public DiscordReceiver(IOptions<AppConfig> options, IEnumerable<ICommand> commands)
    {
        _appConfig = options?.Value;
        _commands = commands;
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
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
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
        Log.Information("{CurrentUser} is connected!", _client.CurrentUser);

        return Task.CompletedTask;
    }

    private Task DiscordLog(LogMessage logMessage)
    {
        Log.Information(logMessage.Message);
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client.CurrentUser.Id)
            return;


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
                    await message.Channel.SendMessageAsync(textResponse?.Message);
                    break;
                }
                case IImageResponse imageResponse:
                {
                    //temporary
                    await message.Channel.SendMessageAsync(imageResponse?.SourceUrl);
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
}