using Pos_System.API.Enums;
using Pos_System.API.Utils;

namespace Pos_System.API.Payload.Response.Products
{
    public class GetProductDetailsResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public double SellingPrice { get; set; }
        public string? PicUrl { get; set; }
        public ProductStatus Status { get; set; }
        public double HistoricalPrice { get; set; }
        public double DiscountPrice { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public string? Size { get; set; }
        public ProductType Type { get; set; }
        public Guid? ParentProductId { get; set; }
        public Guid BrandId { get; set; }
        public Guid CategoryId { get; set; }

        public GetProductDetailsResponse(Guid id, string code, string name, double sellingPrice, string? picUrl, string status, double historicalPrice, double discountPrice, string? description, int displayOrder, string? size, string type, Guid? parentProductId, Guid brandId, Guid categoryId)
        {
            Id = id;
            Code = code;
            Name = name;
            SellingPrice = sellingPrice;
            PicUrl = picUrl;
            Status = EnumUtil.ParseEnum<ProductStatus>(status); 
            HistoricalPrice = historicalPrice;
            DiscountPrice = discountPrice;
            Description = description;
            DisplayOrder = displayOrder;
            Size = size;
            Type = EnumUtil.ParseEnum<ProductType>(type); 
            ParentProductId = parentProductId;
            BrandId = brandId;
            CategoryId = categoryId;
        }
    }
}

