using AutoMapper;
using Pos_System.API.Payload.Request.BlogPost;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.Domain.Models;

namespace Pos_System.API.Mappers.BlogPosts
{
    public class BlogPostMapper : Profile
    {
        public BlogPostMapper()
        {
            CreateMap<CreateBlogPostRequest, BlogPost>();
            CreateMap<BlogPost, CreateBlogPostResponse>();
        }
    }
}
