namespace Pos_System.API.Payload.Response.Products;

public class VariantDetailsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Value { get; set; }
    public int? DisplayOrder { get; set; }
}