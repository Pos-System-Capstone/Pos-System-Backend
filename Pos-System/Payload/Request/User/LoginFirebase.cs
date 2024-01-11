namespace Pos_System.API.Payload.Request.User
{
    public class LoginFirebase
    {
        public string Token { get; set; } = null!;
        public string BrandCode { get; set; } = null!;

        public string? FcmToken { get; set; }
    }
}