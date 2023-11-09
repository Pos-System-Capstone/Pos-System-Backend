using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class PromotionOrderMapping
    {
        public Guid Id { get; set; }
        public Guid PromotionId { get; set; }
        public Guid OrderId { get; set; }
        public double? DiscountAmount { get; set; }
        public int? Quantity { get; set; }
        public Guid? OrderDetailId { get; set; }
        public string? EffectType { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Promotion Promotion { get; set; } = null!;
    }
}
