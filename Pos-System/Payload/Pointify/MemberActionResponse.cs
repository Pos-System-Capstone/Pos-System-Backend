namespace Pos_System.API.Payload.Pointify;

public class MemberActionResponse
{
    public Guid Id { get; set; }
    public double ActionValue { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public Guid MemberWalletId { get; set; }
    public Guid MemberActionTypeId { get; set; }
    
    public Guid? TransactionId { get; set; }
}