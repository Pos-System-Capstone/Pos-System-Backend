using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Transaction
    {
        public Guid Id { get; set; }
        public string? TransactionJson { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? UserId { get; set; }
        public string Status { get; set; } = null!;
        public Guid BrandId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public Guid? BrandPartnerId { get; set; }
        public bool? IsIncrease { get; set; }
        public string? Type { get; set; }

        public virtual Brand Brand { get; set; } = null!;
    }
}
