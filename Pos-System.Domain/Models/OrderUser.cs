using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class OrderUser
    {
        public OrderUser()
        {
            Orders = new HashSet<Order>();
        }

        public Guid Id { get; set; }
        public string? UserType { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public Guid? UserId { get; set; }
        public string? Note { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? PaymentStatus { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
