using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class VariantProductMapping
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public Guid ProductId { get; set; }

        public virtual Product Product { get; set; } = null!;
        public virtual Variant Variant { get; set; } = null!;
    }
}
