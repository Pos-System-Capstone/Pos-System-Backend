namespace Pos_System.API.Payload.Pointify;

public class MemberActionRequest
{
    public Guid ApiKey { get; set; }
    public Guid MembershipId { get; set; }
    public double Amount { get; set; }
    public string MemberActionType { get; set; }
    public string Description { get; set; }
}