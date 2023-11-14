using Pos_System.API.Payload.Pointify;

namespace Pos_System.API.Payload.Response.User
{
    public class SignInResponse
    {
        public string message { get; set; }
        public string AccessToken { get; set; }
        public UserResponse UserInfo { get; set; }
    }

    public class UserResponse
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = null!;
        public string FireBaseUid { get; set; } = null!;
        public string? Fcmtoken { get; set; }
        public Guid BrandId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UrlImg { get; set; }
        public List<MemberWallet>? Wallets { get; set; } = new List<MemberWallet>();
        public MemberLevel? Level { get; set; }
    }
}