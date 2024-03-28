using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class Store
    {
        public Store()
        {
            MenuStores = new HashSet<MenuStore>();
            Sessions = new HashSet<Session>();
            StoreAccounts = new HashSet<StoreAccount>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid BrandId { get; set; }
        public string? Address { get; set; }
        public string? WifiName { get; set; }
        public string? WifiPassword { get; set; }
        public string? Lat { get; set; }
        public string? Long { get; set; }
        public int? Index { get; set; }
        public string? LocationNearby { get; set; }

        public virtual Brand Brand { get; set; } = null!;
        public virtual ICollection<MenuStore> MenuStores { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }
        public virtual ICollection<StoreAccount> StoreAccounts { get; set; }
    }
}
