using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class BrandPartner
    {
        public Guid Id { get; set; }
        public Guid MasterBrandId { get; set; }
        public Guid BrandPartnerId { get; set; }
        public double DebtBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = null!;
        public string Type { get; set; } = null!;

        public virtual Brand BrandPartnerNavigation { get; set; } = null!;
        public virtual Brand MasterBrand { get; set; } = null!;
    }
}
