namespace Pos_System.API.Payload.Request.User;

public class ZaloCallbackRequest
{
    public Data Data { get; set; }
    public string? Mac { get; set; }
}

public class Data
{
    public string? AppId { get; set; }
    public string? OrderId { get; set; }
    public string? Method { get; set; }
}