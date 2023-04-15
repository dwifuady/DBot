using Refit;

namespace DBot.Services.SiCepat;

public interface ISiCepatApi
{
    [Get("/public/check-awb/{id}")]
    Task<SiCepatDto> CheckAwbAsync(string id);
}
