namespace DBot.Shared;

public interface IFileResponse : IResponse
{
    string SourceUrl { get; }
    string? Caption { get; }
}
