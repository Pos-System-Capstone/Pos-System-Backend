using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Response.Orders
{
    public class GetOrderDetailResponse
    {
        public Guid OrderId { get; set; }
        public string InvoiceId { get; set; }

        public string StoreName { get; set; }
        public double TotalAmount { get; set; }
        public double FinalAmount { get; set; }
        public double Vat { get; set; }
        public double VatAmount { get; set; }
        public double Discount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public OrderType OrderType { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public DateTime CheckInDate { get; set; }

        public int? CustomerNumber { get; set; }

        public List<OrderPromotionResponse> PromotionList { get; set; } = new List<OrderPromotionResponse>();

        public List<OrderProductDetailResponse> ProductList { get; set; } = new List<OrderProductDetailResponse>();
        public OrderUserResponse? CustomerInfo { get; set; }
    }

    public class OrderProductDetailResponse
    {
        public Guid ProductInMenuId { get; set; }
        public Guid OrderDetailId { get; set; }
        public double SellingPrice { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; }
        public double TotalAmount { get; set; }
        public double FinalAmount { get; set; }
        public double Discount { get; set; }
        public string Note { get; set; }


        public List<OrderProductExtraDetailResponse> Extras { get; set; } = new List<OrderProductExtraDetailResponse>();
    }

    public class OrderProductExtraDetailResponse
    {
        public Guid ProductInMenuId { get; set; }
        public double SellingPrice { get; set; }
        public int Quantity { get; set; }
        public double TotalAmount { get; set; }
        public double FinalAmount { get; set; }
        public double Discount { get; set; }
        public string Name { get; set; }
    }

    public class OrderPromotionResponse
    {
        public Guid PromotionId { get; set; }

        public string PromotionName { get; set; }
        public double DiscountAmount { get; set; }
        public int Quantity { get; set; }

        public string? EffectType { get; set; }
    }

    public class OrderUserResponse
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public string? CustomerType { get; set; }

        public string? DeliTime { get; set; }

        public PaymentStatusEnum? PaymentStatus { get; set; }

        public OrderSourceStatus? DeliStatus { get; set; }
    }
}