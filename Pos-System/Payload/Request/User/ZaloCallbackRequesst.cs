namespace Pos_System.API.Payload.Request.User;

public class ZaloCallbackRequest
{
    public ZaloCallbackRequest(string appId, string orderId, string method)
    {
        appId = appId;
        orderId = orderId;
        method = method;
    }

    public string appId { get; set; }
    public string orderId { get; set; }
    public string method { get; set; }
}