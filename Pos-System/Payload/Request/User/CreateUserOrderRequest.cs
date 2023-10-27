using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.User;

public class CreateUserOrderRequest
{
    public Guid StoreId { get; set; }
    public Guid UserId { get; set; }
    public string? Address { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentTypeEnum PaymentType { get; set; }
    public List<OrderProduct> ProductsList { get; set; } = new List<OrderProduct>();
    public double TotalAmount { get; set; }
    public double DiscountAmount { get; set; }
    public double FinalAmount { get; set; }
    public List<OrderPromotion>? PromotionList { get; set; } = new List<OrderPromotion>();
}

public class OrderProduct
{
    public Guid ProductInMenuId { get; set; }
    public int Quantity { get; set; }
    public float SellingPrice { get; set; }
    public double Discount { get; set; }
    public string? Note { get; set; }
    public Guid? PromotionId { get; set; }
    public List<OrderProductExtra> Extras { get; set; } = new List<OrderProductExtra>();
}

public class OrderProductExtra
{
    public Guid ProductInMenuId { get; set; }
    public int Quantity { get; set; }
    public float SellingPrice { get; set; }
    public double Discount { get; set; }
}

public class OrderPromotion
{
    public Guid PromotionId { get; set; }
    public double DiscountAmount { get; set; }
}