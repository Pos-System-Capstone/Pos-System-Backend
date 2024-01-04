namespace Pos_System.API.Payload.Request.User;

public class LoginMiniApp
{
    public string Phone { get; set; } = null!;
    public string BrandCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
}