namespace DBot.Shared;

public class TextResponse : ITextResponse
{
    public TextResponse(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
        IsSupportConversation = false;
    }

    public TextResponse(bool isSuccess, string message, bool isSupportConversation) : this(isSuccess, message)
    {
        IsSupportConversation = isSupportConversation;
    }

    public string? Message { get; }
    public bool IsSuccess { get; }

    public bool? IsSupportConversation { get;}
}
