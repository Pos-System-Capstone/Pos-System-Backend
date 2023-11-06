using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.CheckoutOrder
{
    public class CheckoutOrderRequest
    {
        public CheckoutOrderRequest()
        {
            Gift = new List<Object>();
        }
        public List<Effect> Effects { get; set; }
        public CustomerOrderInfo CustomerOrderInfo { get; set; }
        public List<object> Gift { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? DiscountOrderDetail { get; set; }
        public decimal? FinalAmount { get; set; }
        public decimal? BonusPoint { get; set; }
    }
    public class Effect
    {
        public Guid PromotionId { get; set; }
        public Guid? PromotionTierId { get; set; }
        public int? TierIndex { get; set; }
        public string? PromotionName { get; set; }
        public string? ConditionRuleName { get; set; }
        public string? ImgUrl { get; set; }
        public string? Description { get; set; }
        public string? EffectType { get; set; }
        public int? PromotionType { get; set; }
        public Props Prop { get; set; }
    }
    public class Props
    {
        public string Code { get; set; }
        public decimal Value { get; set; }
    }
    public class CustomerOrderInfo
    {
        public CustomerOrderInfo()
        {
            CartItems = new List<Item>();
            Users = new Users();
            Vouchers = new List<CouponCode>();
        }

        public string ApiKey { get; set; }
        public string Id { get; set; }
        public DateTime BookingDate { get; set; }

        public OrderAttribute Attributes { get; set; }
        public List<Item> CartItems { get; set; }

        public List<CouponCode> Vouchers { get; set; }
        public decimal Amount { get; set; }
        public decimal ShippingFee { get; set; }
        public Users Users { get; set; }
    }
    public class Item
    {
        [StringLength(20)] public string ProductCode { get; set; }
        [StringLength(20)] public string CategoryCode { get; set; }
        [StringLength(100)] public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountFromOrder { get; set; }
        public decimal Total { get; set; }
        [StringLength(1000)] public string UrlImg { get; set; }
        public string PromotionCodeApply { get; set; }
    }
    public class CouponCode
    {
        public string PromotionCode { get; set; }
        public string VoucherCode { get; set; }
    }
    public class Users
    {
        public Guid MembershipId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNo { get; set; }
        public int UserGender { get; set; } = 3;
        public string UserLevel { get; set; }
    }
    public class OrderAttribute
    {
        public int SalesMode { get; set; }
        public int PaymentMethod { get; set; }
        public StoreInfo StoreInfo { get; set; }
        public ChannelInfo ChannelInfo { get; set; }
    }
    public class StoreInfo
    {
        public string StoreCode { get; set; }
        [StringLength(100)] public string BrandCode { get; set; }
        public string Applier { get; set; }
    }
    public class ChannelInfo
    {
        public string ChannelCode { get; set; }
        [StringLength(100)] public string BrandCode { get; set; }
        public string Applier { get; set; }
    }


}
