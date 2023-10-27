namespace Pos_System.API.Payload.Response.BlogPost
{
    public class CreateBlogPostResponse
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
    }
}
