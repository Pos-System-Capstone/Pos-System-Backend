namespace Pos_System.API.Payload.Response.BlogPost
{
    public class GetBlogPostResponse
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

        public GetBlogPostResponse(Guid id, string title, string? blogContent, Guid? brandId, string? image, bool? isDialog, string? metaData, string status, short priority)
        {
            Id = id;
            Title = title;
            BlogContent = blogContent;
            BrandId = brandId;
            Image = image;
            IsDialog = isDialog;
            MetaData = metaData;
            Status = status;
            Priority = priority;
        }
    }
}
