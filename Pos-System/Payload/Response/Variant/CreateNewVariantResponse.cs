namespace Pos_System.API.Payload.Response.Variant
{
    public class CreateNewVariantResponse
    {
        public Guid Id { get; set; }
        public CreateNewVariantResponse(Guid id)
        {
            Id = id;
        }
    }
}
