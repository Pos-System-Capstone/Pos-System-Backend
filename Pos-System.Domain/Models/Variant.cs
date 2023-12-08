using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Variant
    {
        public Variant()
        {
            VariantOptions = new HashSet<VariantOption>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid BrandId { get; set; }

        public virtual ICollection<VariantOption> VariantOptions { get; set; }
    }
}
