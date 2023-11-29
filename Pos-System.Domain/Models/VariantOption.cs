using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class VariantOption
    {
        public Guid Id { get; set; }
        public string Percentage { get; set; } = null!;
    }
}
