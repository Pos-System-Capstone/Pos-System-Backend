using Pos_System.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.Products
{
    public class UpdateProductRequest
    {
        [MaxLength(50, ErrorMessage = "Tên của product phải dưới 50 kí tự")]
        public string Name { get; set; }
        [MaxLength(20, ErrorMessage = "Code của product phải dưới 20 kí tự")]
        public string Code { get; set; }
        [Required]
        public double SellingPrice { get; set; }
        public string? PicUrl { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
        [Required]
        public double HistoricalPrice { get; set; }
        public double? DiscountPrice { get; set; }
        public string? Description { get; set; }
        [Required]
        public int DisplayOrder { get; set; }
        public ProductSize? Size { get; set; }
        [Required]
        public ProductType Type { get; set; }
        public Guid? ParentProductId { get; set; }
        
        public string? Status { get; set; }
    }
}
