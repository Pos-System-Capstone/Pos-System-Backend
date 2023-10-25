using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Payment
    {
        public Guid Id { get; set; }
        public Guid? OrderId { get; set; }
        public double Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string? Notes { get; set; }
        public DateTime PayTime { get; set; }
        public string Status { get; set; } = null!;
        public string Type { get; set; } = null!;
        public Guid? SourceId { get; set; }
    }
}
