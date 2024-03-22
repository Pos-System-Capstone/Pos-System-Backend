using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.Products;

public class UpdateProductVariantRequest
{
    public string? Name { get; set; }
    public string? Value { get; set; }

    public int? DisplayOrder { get; set; }
    public ProductStatus? Status { get; set; }
}