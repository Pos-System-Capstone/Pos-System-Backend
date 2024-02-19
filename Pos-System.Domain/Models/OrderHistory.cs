using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class OrderHistory
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public DateTime CreatedTime { get; set; }
        public string FromStatus { get; set; } = null!;
        public string ToStatus { get; set; } = null!;
        public Guid? ChangedBy { get; set; }
        public string? Note { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
