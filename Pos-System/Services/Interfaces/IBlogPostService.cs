using Pos_System.API.Payload.Request.BlogPost;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IBlogPostService
    {
        Task<IPaginate<GetBlogPostResponse>> GetBlogPostByBrandCode(string? brandCode, int page, int size);

        Task<IPaginate<GetBlogPostResponse>> GetBlogPost(int page, int size);

        Task<CreateBlogPostResponse> CreateNewBlogPost(CreateBlogPostRequest createNewBlogPostRequest);

        Task<bool> RemovedBlogPostById(Guid blogId);

        Task<bool> UpdateBlogPost(Guid id, UpdateBlogPostRequest request);

        Task<BlogPost> GetBlogDetails(Guid id);
    }
}