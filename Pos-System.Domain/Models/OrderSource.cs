using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class OrderSource
    {
        public OrderSource()
        {
            Orders = new HashSet<Order>();
        }

        public Guid Id { get; set; }
        public string? SourceType { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid SourceId { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
