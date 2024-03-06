namespace Pos_System.API.Payload.Request.Menus
{
    public class UpdateMenuProductsRequest
    {
        public List<ProductToUpdate> Products { get; set; } = new List<ProductToUpdate>();
    }

    public class ProductToUpdate
    {
        public Guid ProductId { get; set; }
        public double? SellingPrice { get; set; }
        public double? DiscountPrice { get; set; }
    }
}
