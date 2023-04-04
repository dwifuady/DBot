# DBot
## Prerequisites
- .NET 7.0

## Running Locally
### Telegram
- [Create Telegram Bot](https://core.telegram.org/bots#how-do-i-create-a-bot)
- Put the token to the appsettings.json > AppConfig>TelegramConfig>Token  
  or set it to user secrets  
  `dotnet user-secrets set "AppConfig:TelegramConfig:Token" "YOUR_TOKEN"`
### Discord
- [Create Discord Bot here](https://discord.com/developers/applications)
- Put the token to the appsettings.json > AppConfig>DiscordConfig>Token  
  or set it to user secrets  
  `dotnet user-secrets set "AppConfig:DiscordConfig:Token" "YOUR_TOKEN"`

### Services - OpenAi
- Register an account to the openai platform
- Put the token to the appsettings.json > OpenAIConfig>Token  
  or set it to user secrets  
  `dotnet user-secrets set "AppConfig:OpenAIConfig:Token" "YOUR_TOKEN"`

Run the console project