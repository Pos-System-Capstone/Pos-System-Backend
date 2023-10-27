using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.BlogPost;
using Pos_System.API.Payload.Request.User;
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

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.BlogPost.GetBlogPostByBrandCodeEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBlogPostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlogPostByBrandCode([FromQuery] string? brandCode, [FromQuery] int page, [FromQuery] int size)
        {
            var blogPostInBrand = await _blogPostService.GetBlogPostByBrandCode(brandCode, page, size);
            return Ok(blogPostInBrand);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.BlogPost.BlogPostEndpoint)]
        [ProducesResponseType(typeof(GetBlogPostResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlogPostById(Guid id)
        {
            var getBlogById = await _blogPostService.GetBlogPostById(id);
            return Ok(getBlogById);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.BlogPost.BlogPostsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBlogPostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllBlog()
        {
            var getAllBlog = await _blogPostService.GetAllBlog();
            return Ok(getAllBlog);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpPost(ApiEndPointConstant.BlogPost.BlogPostsEndpoint)]
        public async Task<IActionResult> CreateNewBlogPost([FromBody] CreateBlogPostRequest createNewBlogPostRequest, [FromQuery]Guid? brandId)
        {
            _logger.LogInformation($"Start to create new blog post with {createNewBlogPostRequest}");
            var response = await _blogPostService.CreateNewBlogPost(createNewBlogPostRequest, brandId);
            if (response == null)
            {
                _logger.LogInformation(
                    $"Create new blog post failed: {createNewBlogPostRequest.Title}, {createNewBlogPostRequest.BlogContent}");
                return Ok(MessageConstant.BlogPost.CreateNewBlogPostFailedMessage);
            }
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
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
        public async Task<IActionResult> UpdateBlogPostInformation(Guid id, UpdateUserRequest updatelogpost)
        {
            bool isSuccessfuly = await _blogPostService.UpdateBlogPost(id, updatelogpost);
            if (!isSuccessfuly) return BadRequest(MessageConstant.BlogPost.UpdateBlogPostFailedMessage);
            return Ok(MessageConstant.BlogPost.UpdateBlogPostSuccessfulMessage);
        }
    }
}
