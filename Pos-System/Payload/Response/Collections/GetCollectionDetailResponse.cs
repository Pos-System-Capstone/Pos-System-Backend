using Pos_System.API.Enums;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Payload.Response.Collections
{
    public class GetCollectionDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public CollectionStatus Status { get; set; }
        public string? PicUrl { get; set; }
        public string? Description { get; set; }

        public IPaginate<ProductOfCollection> Products { get; set; }

        public BrandOfCollection brand { get; set; }

        public GetCollectionDetailResponse(Guid id, string name, string code, CollectionStatus status, string? picUrl, string? description)
        {
            Id = id;
            Name = name;
            Code = code;
            Status = status;
            PicUrl = picUrl;
            Description = description;
            Products = null;
        }
        public GetCollectionDetailResponse(Guid id, string name, string code, CollectionStatus status, string? picUrl, string? description, IPaginate<ProductOfCollection> products)
        {
            Id = id;
            Name = name;
            Code = code;
            Status = status;
            PicUrl = picUrl;
            Description = description;
            Products = products;
        }
    }

    public class ProductOfCollection
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string ProductCode { get; set; }
        public string? PicUrl { get; set; }
        public double SellingPrice { get; set; }

        public ProductOfCollection(Guid id, string productName, string? description, string productCode, string? picUrl, double sellingPrice)
        {
            Id = id;
            ProductName = productName;
            Description = description;
            ProductCode = productCode;
            PicUrl = picUrl;
            SellingPrice = sellingPrice;
        }
    }

    public class BrandOfCollection
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PicUrl { get; set; }
        public string Status { get; set; } = null!;

        public BrandOfCollection(Guid id, string name, string? email, string? address, string? phone, string? picUrl, string status)
        {
            Id = id;
            Name = name;
            Email = email;
            Address = address;
            Phone = phone;
            PicUrl = picUrl;
            Status = status;
        }
    }
}
