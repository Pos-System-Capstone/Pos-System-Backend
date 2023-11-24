using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Response.Orders;

public class PaymentOrderResponse
{
    public Guid OrderId { get; set; }
    public string PaymentType { get; set; }
    public string Status { get; set; }
    public string? Message { get; set; }
}