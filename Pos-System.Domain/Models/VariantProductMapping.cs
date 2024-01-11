using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class VariantProductMapping
    {
        public Guid Id { get; set; }
        public Guid GroupVariantId { get; set; }
        public Guid ProductId { get; set; }
    }
}
