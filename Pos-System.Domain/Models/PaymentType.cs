using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class PaymentType
    {
        public PaymentType()
        {
            BrandPaymentMappings = new HashSet<BrandPaymentMapping>();
        }

        public string Type { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PicUrl { get; set; } = null!;
        public string PaymentCode { get; set; } = null!;
        public string Status { get; set; } = null!;

        public virtual ICollection<BrandPaymentMapping> BrandPaymentMappings { get; set; }
    }
}
