namespace Pos_System.API.Payload.Pointify;

public class MemberDetailsResponse
{
    public Guid MembershipId { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Fullname { get; set; }
    public Guid MemberLevelId { get; set; }
    public long Gender { get; set; }
    public MemberLevel MemberLevel { get; set; }
    public List<MemberWallet> MemberWallet { get; set; }
}

public class MemberLevel
{
    public Guid MemberLevelId { get; set; }
    public string Name { get; set; }
    public long IndexLevel { get; set; }
    public object Benefits { get; set; }
}

public class MemberWallet
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public long Balance { get; set; }
    public long BalanceHistory { get; set; }
}