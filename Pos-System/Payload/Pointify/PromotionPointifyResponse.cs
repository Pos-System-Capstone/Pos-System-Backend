namespace Pos_System.API.Payload.Pointify;

public class PromotionPointifyResponse
{
    public Guid PromotionId { get; set; }
    public Guid PromotionTierId { get; set; }
    public string PromotionName { get; set; }
    public string PromotionCode { get; set; }
    public string Description { get; set; }
    public long ForMembership { get; set; }
    public long ActionType { get; set; }
    public long SaleMode { get; set; }
    public string ImgUrl { get; set; }
    public long PromotionType { get; set; }
    public long TierIndex { get; set; }
    public DateTimeOffset EndDate { get; set; }
}