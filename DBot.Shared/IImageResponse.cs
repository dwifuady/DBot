namespace DBot.Shared;

public interface IImageResponse : IResponse
{
    string SourceUrl { get; }
    string? Caption { get; }
}
