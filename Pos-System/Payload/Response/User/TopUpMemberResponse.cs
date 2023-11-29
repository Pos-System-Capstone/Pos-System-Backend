namespace Pos_System.API.Payload.Response.User;

public class TopUpUserWalletResponse
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string PaymentType { get; set; }
    public string Status { get; set; }
    public string? Message { get; set; }
}