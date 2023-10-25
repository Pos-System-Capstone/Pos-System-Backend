using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
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
            var blogPostInBrand = _blogPostService.GetBlogPostByBrandCode(brandCode, page, size);
            return Ok(blogPostInBrand);
        }
    }
}
