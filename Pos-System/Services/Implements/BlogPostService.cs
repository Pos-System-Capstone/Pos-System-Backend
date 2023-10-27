using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.BlogPost;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
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

        public async Task<IPaginate<GetBlogPostResponse>> GetBlogPost(int page, int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            IPaginate<GetBlogPostResponse> blogPostResponse = await _unitOfWork.GetRepository<BlogPost>().GetPagingListAsync(
                selector: x => new GetBlogPostResponse(x.Id, x.Title, x.BlogContent, x.BrandId, x.Image, x.IsDialog, x.MetaData, x.Status, x.Priority),
                predicate: x => x.BrandId.Equals(brand.Id) && x.Status.Equals(BlogPostStatus.Active.GetDescriptionFromEnum()),
                page: page,
                size: size,
                orderBy: x => x.OrderByDescending(x => x.Priority));

            return blogPostResponse;
        }

        public async Task<IPaginate<GetBlogPostResponse>> GetBlogPostByBrandCode(string? brandCode, int page, int size)
        {
            if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandCodeNotFoundMessage);

            IPaginate<GetBlogPostResponse> blogPostResponse = await _unitOfWork.GetRepository<BlogPost>().GetPagingListAsync(
                selector: x => new GetBlogPostResponse(x.Id, x.Title, x.BlogContent, x.BrandId, x.Image, x.IsDialog, x.MetaData, x.Status, x.Priority),
                predicate: x => x.BrandId.Equals(brand.Id) && x.Status.Equals(BlogPostStatus.Active.GetDescriptionFromEnum()),
                page: page,
                size: size,
                orderBy: x => x.OrderByDescending(x => x.Priority));

            return blogPostResponse;
        }

        public async Task<CreateBlogPostResponse> CreateNewBlogPost(CreateBlogPostRequest createNewBlogPostRequest)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            _logger.LogInformation($"Start create new : {createNewBlogPostRequest}");
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            BlogPost newBlogPost = new BlogPost()
            {
                Id = Guid.NewGuid(),
                Title = createNewBlogPostRequest.Title,
                BlogContent = createNewBlogPostRequest.BlogContent,
                BrandId = brandId,
                Image = createNewBlogPostRequest.Image,
                IsDialog = createNewBlogPostRequest.IsDialog,
                MetaData = createNewBlogPostRequest.MetaData,
                Status = EnumUtil.GetDescriptionFromEnum(BlogPostStatus.Active),
                Priority = createNewBlogPostRequest.Priority,

            };
            await _unitOfWork.GetRepository<BlogPost>().InsertAsync(newBlogPost);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            CreateBlogPostResponse blogPostResponse = null;
            if (isSuccessful)
            {
                blogPostResponse = _mapper.Map<CreateBlogPostResponse>(newBlogPost);
            }
            return blogPostResponse;
        }

        public async Task<bool> RemovedBlogPostById(Guid blogId)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (blogId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.BlogPost.EmptyBlogIdMessage);
            BlogPost removedBlog = await _unitOfWork.GetRepository<BlogPost>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(blogId));
            if (removedBlog == null) throw new BadHttpRequestException(MessageConstant.BlogPost.BlogNotFoundMessage);
            _logger.LogInformation($"Start remove blog {blogId}");
            removedBlog.Status = "Deactivate";
            _unitOfWork.GetRepository<BlogPost>().UpdateAsync(removedBlog);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }

        public async Task<bool> UpdateBlogPost(Guid id, UpdateBlogPostRequest request)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (id == Guid.Empty && id == null) throw new BadHttpRequestException(MessageConstant.BlogPost.EmptyBlogIdMessage);
            BlogPost blogpost = await _unitOfWork.GetRepository<BlogPost>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(id)
                );
            if (blogpost == null) throw new BadHttpRequestException(MessageConstant.BlogPost.BlogNotFoundMessage);

            blogpost.Title = request.Title;
            blogpost.BlogContent = request.BlogContent;
            blogpost.Image = request.Image;

            _unitOfWork.GetRepository<BlogPost>().UpdateAsync(blogpost);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;

        }
    }
}
