namespace Pos_System.API.Payload.Pointify;

public class VoucherResponse
{
    public Guid VoucherId { get; set; }
    public string VoucherCode { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? VoucherGroupId { get; set; }
    public Guid? MembershipId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRedemped { get; set; }
    public DateTime? UsedDate { get; set; }
    public DateTime? RedempedDate { get; set; }
    public DateTime? InsDate { get; set; }
    public DateTime? UpdDate { get; set; }
    public Guid? PromotionId { get; set; }
    public int Index { get; set; }
    public Guid? PromotionTierId { get; set; }
}