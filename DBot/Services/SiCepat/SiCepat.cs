using DBot.Shared;

namespace DBot.Services.SiCepat;

public class SiCepat : ICommand
{
    private readonly ISiCepatApi _siCepatApi;

    public IReadOnlyList<string> AcceptedCommands => new List<string> 
    {
        "SICEPAT"
    };

    public SiCepat(ISiCepatApi siCepatApi)
    {
        _siCepatApi = siCepatApi;
    }

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        var response = await _siCepatApi.CheckAwbAsync(request.Args);
        var message = await GetMessage(response.Sicepat.Result);
        return new TextResponse(true, message);
    }

    private static async Task<string> GetMessage(Result result)
    {
        if (result.Delivered)
        {
            return await Task.Run(() =>
                @$"{result.WaybillNumber}
From {result.Sender} - {result.SenderAddress} at {result.SendDate} has been Delivered.
{result.PODReceiver} : {result.PODReceiverTime}"
            );
        }

        return await Task.Run(() =>
            @$"{result.WaybillNumber}
From {result.Sender} - {result.SenderAddress} at {result.SendDate} 
Current status
{result.LastStatus.DateTime}: {result.LastStatus.ReceiverName ?? result.LastStatus.City}"
        );
    }
}
