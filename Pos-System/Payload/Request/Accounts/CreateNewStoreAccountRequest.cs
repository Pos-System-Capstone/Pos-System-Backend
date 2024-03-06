using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.Accounts
{
    public class CreateNewStoreAccountRequest
    {
        [Required(ErrorMessage = "Username is missing")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Name is missing")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Password is missing")]
        public string Password { get; set; }

        public CreateNewStoreAccountRequest()
        {

        }
    }
}
