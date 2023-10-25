using AutoMapper;
using Pos_System.API.Services.Interfaces;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class BlogPostService : BaseService<BlogPostService>, IBlogPostService
    {
        public BlogPostService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<BlogPostService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }


    }
}
