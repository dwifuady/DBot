namespace DBot.Shared;

public interface ITextResponse : IResponse
{
    string? Message { get; set; }
}
