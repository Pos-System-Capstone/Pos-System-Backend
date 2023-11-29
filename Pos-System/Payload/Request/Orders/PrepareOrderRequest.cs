using Newtonsoft.Json;
using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.Orders;

public class PrepareOrderRequest
{
    public Guid StoreId { get; set; }

    public OrderType OrderType { get; set; }

    public PaymentTypeEnum PaymentType { get; set; }
    public List<ProductList> ProductList { get; set; } = new List<ProductList>();
    public double TotalAmount { get; set; }
    public double DiscountAmount { get; set; }
    public double ShippingFee { get; set; }
    public double FinalAmount { get; set; }
    public double BonusPoint { get; set; }
    public string? PromotionCode { get; set; }
    public string? VoucherCode { get; set; }
    public List<PromotionPrepare>? PromotionList { get; set; } = new List<PromotionPrepare>();
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? DeliveryAddress { get; set; }
    public string? Message { get; set; }
}

public class ProductList
{
    public Guid ProductInMenuId { get; set; }

    public Guid? ParentProductId { get; set; }

    public string Name { get; set; }

    public ProductType Type { get; set; }
    public int Quantity { get; set; }
    public float SellingPrice { get; set; }
    public string Code { get; set; }
    public string CategoryCode { get; set; }

    public double TotalAmount { get; set; }
    public double Discount { get; set; }
    public double FinalAmount { get; set; }

    public string? PromotionCodeApplied { get; set; }

    public string? Note { get; set; }
    public string? PicUrl { get; set; }
    public List<ExtraProduct> Extras { get; set; } = new List<ExtraProduct>();

    public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
}

public class ExtraProduct
{
    public Guid ProductInMenuId { get; set; }

    public string Name { get; set; }
    public int Quantity { get; set; }
    public float SellingPrice { get; set; }
    public double TotalAmount { get; set; }
}

public class ProductAttribute
{
    public string Name { get; set; }

    public string Value { get; set; }
}

public class PromotionPrepare
{
    public Guid? PromotionId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public double? DiscountAmount { get; set; }
    public string? EffectType { get; set; }
}