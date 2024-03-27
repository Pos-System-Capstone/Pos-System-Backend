namespace Pos_System.API.Payload.Request.User
{
    public class CreateNewUserRequest
    {
        public string PhoneNunmer { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string? Email { get; set; }
        public string? FireBaseUid { get; set; }

        public string? FcmToken { get; set; }
        public string? PinCode { get; set; }
    }
}