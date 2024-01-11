namespace Pos_System.API.Payload.Request.User;

public class MemberLoginRequest
{
    public string Phone { get; set; } = null!;
    public string PinCode { get; set; } = null!;
    public string Method { get; set; } = null!;
    public string BrandCode { get; set; } = null!;
}