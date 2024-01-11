using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class User
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
        public string? PinCode { get; set; }

        public virtual Brand Brand { get; set; } = null!;
    }
}
