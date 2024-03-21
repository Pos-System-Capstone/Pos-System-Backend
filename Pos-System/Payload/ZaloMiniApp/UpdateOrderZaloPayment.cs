namespace Pos_System.API.Payload.ZaloMiniApp;

public class UpdateOrderZaloPayment
{
    public string? AppId { get; set; }
    public string? OrderId { get; set; }
    public int ResultCode { get; set; }
    public string? Mac { get; set; }
}