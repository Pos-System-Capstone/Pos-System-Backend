using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class StorePaymentMapping
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public string PaymentType { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string Status { get; set; } = null!;

        public virtual PaymentType PaymentTypeNavigation { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
    }
}
