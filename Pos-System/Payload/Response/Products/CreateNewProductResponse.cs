namespace Pos_System.API.Payload.Response.Products
{
    public class CreateNewProductResponse
    {
        public Guid Id { get; set; }
        public CreateNewProductResponse(Guid id)
        {
            Id = id;
        }
    }
}

 