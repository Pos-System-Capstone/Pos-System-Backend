namespace Pos_System.API.Payload.Request.BlogPost
{
    public class UpdateBlogPostRequest
    {
        public string Title { get; set; } = null!;
        public string? BlogContent { get; set; }
        public string? Image { get; set; }
    }
}
