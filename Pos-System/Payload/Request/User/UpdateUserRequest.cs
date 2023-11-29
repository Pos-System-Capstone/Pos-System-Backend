using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.User
{
    public class UpdateUserRequest
    {
        [Phone(ErrorMessage = "Phone sai format")]
        public string? PhoneNunmer { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        [EmailAddress(ErrorMessage = "Email sai format")]
        public string? Email { get; set; }
        
        public string? UrlImg { get; set; }

        public void TrimString()
        {
            FullName = FullName?.Trim();
            Gender = Gender?.Trim();
            Email = Email?.Trim();
            PhoneNunmer = PhoneNunmer?.Trim();
            UrlImg = UrlImg?.Trim();
        }
    }
}
