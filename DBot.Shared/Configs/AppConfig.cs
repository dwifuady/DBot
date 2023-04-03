namespace DBot.Shared.Configs;

public class AppConfig
{
    public TelegramConfig? TelegramConfig { get; set; }
    public DiscordConfig? DiscordConfig { get; set; }
}

public class DiscordConfig
{
    public string? Token { get; set; }
    public bool Enable { get; set; }
}

public class TelegramConfig
{
    public string? Token { get; set; }
    public bool Enable { get; set; }
}