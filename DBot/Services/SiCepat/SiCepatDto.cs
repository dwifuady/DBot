using System.Text.Json.Serialization;

namespace DBot.Services.SiCepat;

public class LastStatus
{
    [JsonPropertyName("date_time")]
    public string DateTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("receiver_name")]
    public string ReceiverName { get; set; }
}

public class Result
{
    [JsonPropertyName("waybill_number")]
    public string WaybillNumber { get; set; }

    [JsonPropertyName("kodeasal")]
    public object Kodeasal { get; set; }

    [JsonPropertyName("kodetujuan")]
    public object Kodetujuan { get; set; }

    [JsonPropertyName("service")]
    public string Service { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("partner")]
    public string Partner { get; set; }

    [JsonPropertyName("sender")]
    public string Sender { get; set; }

    [JsonPropertyName("sender_address")]
    public string SenderAddress { get; set; }

    [JsonPropertyName("receiver_address")]
    public string ReceiverAddress { get; set; }

    [JsonPropertyName("receiver_name")]
    public string ReceiverName { get; set; }

    [JsonPropertyName("realprice")]
    public int Realprice { get; set; }

    [JsonPropertyName("totalprice")]
    public int Totalprice { get; set; }

    [JsonPropertyName("POD_receiver")]
    public string PODReceiver { get; set; }

    [JsonPropertyName("POD_receiver_time")]
    public string PODReceiverTime { get; set; }

    [JsonPropertyName("send_date")]
    public string SendDate { get; set; }

    [JsonPropertyName("track_history")]
    public List<TrackHistory> TrackHistory { get; set; }

    [JsonPropertyName("last_status")]
    public LastStatus LastStatus { get; set; }

    [JsonPropertyName("perwakilan")]
    public string Perwakilan { get; set; }

    [JsonPropertyName("pop_sigesit_img_path")]
    public object PopSigesitImgPath { get; set; }

    [JsonPropertyName("pod_sigesit_img_path")]
    public string PodSigesitImgPath { get; set; }

    [JsonPropertyName("pod_sign_img_path")]
    public object PodSignImgPath { get; set; }

    [JsonPropertyName("pod_img_path")]
    public object PodImgPath { get; set; }

    [JsonPropertyName("manifested_img_path")]
    public object ManifestedImgPath { get; set; }

    [JsonIgnore] public bool Delivered => !string.IsNullOrWhiteSpace(PODReceiverTime);
}

public class SiCepatDto
{
    [JsonPropertyName("sicepat")]
    public Sicepat Sicepat { get; set; }

    [JsonPropertyName("sfExpress")]
    public List<object> SfExpress { get; set; }

    [JsonPropertyName("aramex")]
    public List<object> Aramex { get; set; }
}

public class Sicepat
{
    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
}

public class Status
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class TrackHistory
{
    [JsonPropertyName("date_time")]
    public string DateTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }
}

public class Awbs
{
    [JsonPropertyName("awbNo")]
    public List<string> AwbNumbers { get; set; }
}
