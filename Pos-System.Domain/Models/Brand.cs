using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Brand
    {
        public Brand()
        {
            BrandAccounts = new HashSet<BrandAccount>();
            BrandPartnerBrandPartnerNavigations = new HashSet<BrandPartner>();
            BrandPartnerMasterBrands = new HashSet<BrandPartner>();
            Categories = new HashSet<Category>();
            Collections = new HashSet<Collection>();
            Menus = new HashSet<Menu>();
            Products = new HashSet<Product>();
            Promotions = new HashSet<Promotion>();
            Stores = new HashSet<Store>();
            Transactions = new HashSet<Transaction>();
            Users = new HashSet<User>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PicUrl { get; set; }
        public string Status { get; set; } = null!;
        public string? BrandCode { get; set; }
        public double? BrandBalance { get; set; }

        public virtual ICollection<BrandAccount> BrandAccounts { get; set; }
        public virtual ICollection<BrandPartner> BrandPartnerBrandPartnerNavigations { get; set; }
        public virtual ICollection<BrandPartner> BrandPartnerMasterBrands { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Collection> Collections { get; set; }
        public virtual ICollection<Menu> Menus { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Promotion> Promotions { get; set; }
        public virtual ICollection<Store> Stores { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
