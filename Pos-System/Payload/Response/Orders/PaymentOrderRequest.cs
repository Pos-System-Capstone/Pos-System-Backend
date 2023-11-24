using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Response.Orders
{
    public class PaymentOrderRequest
    {
        public string PaymentType { get; set; }
        public string? Code { get; set; }
    }
}