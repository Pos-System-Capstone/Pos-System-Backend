using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Response.Promotion
{
    public class GetPromotionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
        public PromotionEnum Type { get; set; }
        public double? MaxDiscount { get; set; }
        public double? MinConditionAmount { get; set; }
        public double? DiscountAmount { get; set; }
        public double? DiscountPercent { get; set; }
        public bool IsAvailable { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public List<DateFilter> DateFilters { get; set; } = new List<DateFilter>();
        public List<ProductApply> ListProductApply { get; set; } = new List<ProductApply>();
        public string? Status { get; set; }
    }
    public class ProductApply {
        public ProductApply(Guid productId)
        {
            ProductId = productId;
        }

        public Guid ProductId { get; set; }
    }
}

