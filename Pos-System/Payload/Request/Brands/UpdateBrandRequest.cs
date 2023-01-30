using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.Brands
{
    public class UpdateBrandRequest
    {
        [Required(ErrorMessage = "Name must not be empty")]
        [MaxLength(50, ErrorMessage = "Name's max length is 50 characters")]
        public string Name { get; set; }
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PicUrl { get; set; }
    }
}
