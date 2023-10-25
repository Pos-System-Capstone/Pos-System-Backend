using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IBlogPostService
    {
        Task<IPaginate<GetBlogPostResponse>> GetBlogPostByBrandCode(string? brandCode, int page, int size);
    }
}
