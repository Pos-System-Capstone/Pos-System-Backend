namespace Pos_System.API.Payload.Request.User;

public class ZaloCallbackRequesst
{
    public ZaloCallbackRequesst(string data, string mac)
    {
        Data = data;
        Mac = mac;
    }

    public string Data { get; set; }
    public string Mac { get; set; }
}