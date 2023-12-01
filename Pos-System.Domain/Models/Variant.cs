using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Variant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
