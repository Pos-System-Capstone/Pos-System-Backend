namespace Pos_System.API.Payload.Pointify;

public class MemberDetailsResponse
{
    public Guid MembershipId { get; set; }
    public string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Fullname { get; set; }
    public bool DelFlg { get; set; }
    public int Gender { get; set; }
    public MemberLevel MemberLevel { get; set; }
}

public class MemberLevel
{
    public Guid MemberLevelId { get; set; }
    public string? Name { get; set; }
    public int? IndexLevel { get; set; }
    public string? Benefits { get; set; }
    public int? MaxPoint { get; set; }
    public string? NextLevelName { get; set; }
    public List<MemberWallet> MemberWallet { get; set; } = new List<MemberWallet>();
    public List<MembershipCard> MembershipCard { get; set; } = new List<MembershipCard>();
}

public class MembershipCard
{
    public Guid Id { get; set; }
    public string? MembershipCardCode { get; set; }
    public string? PhysicalCardCode { get; set; }
    public MembershipCardType MembershipCardType { get; set; }
}

public class MembershipCardType
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? CardImage { get; set; }
}

public class MemberWallet
{
    public Guid Id { get; set; }
    public double Balance { get; set; }
    public WalletType WalletType { get; set; }
}

public class WalletType
{
    public string Name { get; set; }
    public string Currency { get; set; }
}