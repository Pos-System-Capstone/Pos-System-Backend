using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class ExtraCategory
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
        public Guid ExtraCategoryId { get; set; }
        public Guid ProductCategoryId { get; set; }
        public string? Type { get; set; }

        public virtual Category ExtraCategoryNavigation { get; set; } = null!;
        public virtual Category ProductCategory { get; set; } = null!;
    }
}
