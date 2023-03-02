﻿using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Brand
    {
        public Brand()
        {
            BrandAccounts = new HashSet<BrandAccount>();
            Categories = new HashSet<Category>();
            Collections = new HashSet<Collection>();
            Menus = new HashSet<Menu>();
            OrderSources = new HashSet<OrderSource>();
            PaymentTypes = new HashSet<PaymentType>();
            Products = new HashSet<Product>();
            Stores = new HashSet<Store>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PicUrl { get; set; }
        public string Status { get; set; } = null!;

        public virtual ICollection<BrandAccount> BrandAccounts { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Collection> Collections { get; set; }
        public virtual ICollection<Menu> Menus { get; set; }
        public virtual ICollection<OrderSource> OrderSources { get; set; }
        public virtual ICollection<PaymentType> PaymentTypes { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Store> Stores { get; set; }
    }
}
