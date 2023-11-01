using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Response.Orders
{
    public class PaymentOrderRequest
    {
        public PaymentTypeEnum PaymentType { get; set; }
        public PaymentStatusEnum Status { get; set; }
    }
}
