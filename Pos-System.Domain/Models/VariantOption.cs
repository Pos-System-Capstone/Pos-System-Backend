using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class VariantOption
    {
        public Guid Id { get; set; }
        public string OptionName { get; set; } = null!;
        public Guid VariantId { get; set; }

        public virtual Variant Variant { get; set; } = null!;
    }
}
