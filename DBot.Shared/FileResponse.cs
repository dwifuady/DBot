namespace DBot.Shared;

public class FileResponse : IFileResponse
{
    public FileResponse(bool isSuccess, string sourceUrl, string caption)
    {
        IsSuccess = isSuccess;
        SourceUrl = sourceUrl;
        Caption = caption;
    }
    public string SourceUrl { get; }
    public string? Caption { get; }
    public bool IsSuccess { get; }
}
