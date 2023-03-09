namespace DBot.Shared;

public class ImageResponse : IImageResponse
{
    public ImageResponse(string sourceUrl, string caption)
    {
        SourceUrl = sourceUrl;
        Caption = caption;
    }
    public string SourceUrl { get; }
    public string? Caption { get; }
}
