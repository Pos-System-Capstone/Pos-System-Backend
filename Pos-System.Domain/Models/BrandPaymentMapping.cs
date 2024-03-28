using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class BrandPaymentMapping
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public string PaymentType { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string Status { get; set; } = null!;

        public virtual Brand Brand { get; set; } = null!;
        public virtual PaymentType PaymentTypeNavigation { get; set; } = null!;
    }
}
