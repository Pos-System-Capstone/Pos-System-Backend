using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.API.Services.Interfaces;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class BlogPostService : BaseService<BlogPostService>, IBlogPostService
    {
        public BlogPostService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<BlogPostService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<IPaginate<GetBlogPostResponse>> GetBlogPostByBrandCode(string? brandCode, int page, int size)
        {
            if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandCodeNotFoundMessage);

            IPaginate<GetBlogPostResponse> blogPostResponse = await _unitOfWork.GetRepository<BlogPost>().GetPagingListAsync(
                selector: x => new GetBlogPostResponse(x.Id, x.Title, x.BlogContent, x.BrandId, x.Image, x.IsDialog, x.MetaData, x.Status, x.Priority),
                predicate: x => x.BrandId.Equals(brand.Id));

            return blogPostResponse;
        }
    }
}
