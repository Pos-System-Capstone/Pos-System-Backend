using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.BlogPost;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPostController : BaseController<BlogPostController>
    {
        private readonly IBlogPostService _blogPostService;

        public BlogPostController(ILogger<BlogPostController> logger, IBlogPostService blogPostService) : base(logger)
        {
            _blogPostService = blogPostService;
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.BlogPost.BlogPostsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBlogPostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlogPostByBrandCode([FromQuery] int page, [FromQuery] int size)
        {
            var blogPostInBrand = await _blogPostService.GetBlogPost(page, size);
            return Ok(blogPostInBrand);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.BlogPost.BlogPostsEndpoint)]
        public async Task<IActionResult> CreateNewBlogPost([FromBody] CreateBlogPostRequest createNewBlogPostRequest)
        {
            _logger.LogInformation($"Start to create new blog post with {createNewBlogPostRequest}");
            var response = await _blogPostService.CreateNewBlogPost(createNewBlogPostRequest);
            if (response == null)
            {
                _logger.LogInformation(
                    $"Create new blog post failed: {createNewBlogPostRequest.Title}, {createNewBlogPostRequest.BlogContent}");
                return Ok(MessageConstant.BlogPost.CreateNewBlogPostFailedMessage);
            }

            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.BlogPost.StatusBlogPostEndpoint)]
        public async Task<IActionResult> UpdateUserInformation(Guid id)
        {
            bool isSuccessful = await _blogPostService.RemovedBlogPostById(id);
            if (isSuccessful)
            {
                _logger.LogInformation($"Remove blog {id} information successfully");
                return Ok(MessageConstant.User.UpdateUserSuccessfulMessage);
            }

            _logger.LogInformation($"Remove blog {id} information failed");
            return Ok(MessageConstant.User.UpdateUserFailedMessage);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpPatch(ApiEndPointConstant.BlogPost.BlogPostEndpoint)]
        public async Task<IActionResult> UpdateBlogPostInformation(Guid id, UpdateBlogPostRequest updatelogpost)
        {
            bool isSuccessfuly = await _blogPostService.UpdateBlogPost(id, updatelogpost);
            if (!isSuccessfuly) return BadRequest(MessageConstant.BlogPost.UpdateBlogPostFailedMessage);
            return Ok(MessageConstant.BlogPost.UpdateBlogPostSuccessfulMessage);
        }

        [HttpGet(ApiEndPointConstant.BlogPost.BlogPostEndpoint)]
        public async Task<IActionResult> GetBlogDetail(Guid id)
        {
            var res = await _blogPostService.GetBlogDetails(id);

            return Ok(res);
        }
    }
}