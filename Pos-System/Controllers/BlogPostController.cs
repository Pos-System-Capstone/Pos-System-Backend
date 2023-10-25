using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;

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
        public async Task<IActionResult> GetBlogPostByBrandCode([FromQuery] string? brandCode)
        {
            return Ok();
        }
    }
}
