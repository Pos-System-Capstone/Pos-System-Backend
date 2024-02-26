using Pos_System.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.Stores
{
    public class CreateNewStoreRequest
    {
        [Required(ErrorMessage = "Store name is required")]
        [MaxLength(50, ErrorMessage = "Store's name max length is 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Short name is required")]
        [MaxLength(30, ErrorMessage = "Store's short name max length is 30 characters")]
        public string? ShortName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email")]
        [Required(ErrorMessage = "Store's email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Store's phone is required")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Store's code is required")]
        public string? Code { get; set; }

        public string? Address { get; set; }

        public string? WifiName { get; set; }
        public string? WifiPassword { get; set; }
    }
}