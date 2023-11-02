using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Transaction
    {
        public Guid Id { get; set; }
        public string TransactionJson { get; set; } = null!;
        public DateTime InsDate { get; set; }
        public DateTime UpsDate { get; set; }
        public Guid? VoucherId { get; set; }
        public Guid? PromotionId { get; set; }
        public Guid? MemberActionId { get; set; }
        public Guid BrandId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public Guid? BrandPartnerId { get; set; }
        public bool? IsIncrease { get; set; }
        public string? Type { get; set; }
    }
}
