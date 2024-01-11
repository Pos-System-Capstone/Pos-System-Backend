using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class BlogPost
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? BlogContent { get; set; }
        public Guid? BrandId { get; set; }
        public string? Image { get; set; }
        public bool? IsDialog { get; set; }
        public string? MetaData { get; set; }
        public string Status { get; set; } = null!;
        public short Priority { get; set; }
        public string? Type { get; set; }
    }
}
