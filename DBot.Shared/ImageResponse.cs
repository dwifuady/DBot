namespace DBot.Shared;

public class ImageResponse : IImageResponse
{
    public ImageResponse(bool isSuccess, string sourceUrl, string caption)
    {
        IsSuccess = isSuccess;
        SourceUrl = sourceUrl;
        Caption = caption;
    }
    public string SourceUrl { get; }
    public string? Caption { get; }
    public bool IsSuccess { get; }
}
