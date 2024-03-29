﻿using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Variant
    {
        public Variant()
        {
            VariantProductMappings = new HashSet<VariantProductMapping>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid BrandId { get; set; }
        public string? Value { get; set; }
        public int? DisplayOrder { get; set; }

        public virtual ICollection<VariantProductMapping> VariantProductMappings { get; set; }
    }
}
