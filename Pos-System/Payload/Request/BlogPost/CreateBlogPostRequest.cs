namespace Pos_System.API.Payload.Request.BlogPost
{
    public class CreateBlogPostRequest
    {
        public string Title { get; set; } = null!;
        public string? BlogContent { get; set; }
        public string? Image { get; set; }
        public bool? IsDialog { get; set; }
        public string? MetaData { get; set; }
        public short Priority { get; set; }
    }
}
