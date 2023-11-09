using Pos_System.API.Payload.Request.CheckoutOrder;

namespace Pos_System.API.Payload.Response.CheckoutOrderResponse
{
    public class CheckoutOrderResponse
    {
        public dynamic Code { get; set; }
        public string Message { get; set; }
        public CheckoutOrderRequest Order { get; set; }
    }
}
